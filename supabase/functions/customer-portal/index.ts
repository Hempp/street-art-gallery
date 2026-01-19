// Supabase Edge Function: Stripe Customer Portal
// Deploy: supabase functions deploy customer-portal

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts'
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2'
import Stripe from 'https://esm.sh/stripe@13.10.0?target=deno'

const stripe = new Stripe(Deno.env.get('STRIPE_SECRET_KEY') as string, {
  apiVersion: '2023-10-16',
  httpClient: Stripe.createFetchHttpClient(),
})

const supabaseUrl = Deno.env.get('SUPABASE_URL') as string
const supabaseServiceKey = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') as string

const corsHeaders = {
  'Access-Control-Allow-Origin': '*',
  'Access-Control-Allow-Headers': 'authorization, x-client-info, apikey, content-type',
}

serve(async (req) => {
  // Handle CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response('ok', { headers: corsHeaders })
  }

  try {
    // Get the authorization header
    const authHeader = req.headers.get('Authorization')
    if (!authHeader) {
      throw new Error('No authorization header')
    }

    // Create Supabase client with user's JWT
    const supabase = createClient(supabaseUrl, supabaseServiceKey, {
      global: { headers: { Authorization: authHeader } },
    })

    // Get the user
    const { data: { user }, error: userError } = await supabase.auth.getUser()
    if (userError || !user) {
      throw new Error('Not authenticated')
    }

    // Get Stripe customer ID
    const { data: customer, error: customerError } = await supabase
      .from('customers')
      .select('stripe_customer_id')
      .eq('id', user.id)
      .single()

    if (customerError || !customer?.stripe_customer_id) {
      throw new Error('No subscription found')
    }

    // Parse request body for return URL
    let returnUrl = `${req.headers.get('origin')}/account`
    try {
      const body = await req.json()
      if (body.returnUrl) {
        returnUrl = body.returnUrl
      }
    } catch {
      // No body, use default
    }

    // Create portal session
    const portalSession = await stripe.billingPortal.sessions.create({
      customer: customer.stripe_customer_id,
      return_url: returnUrl,
    })

    return new Response(
      JSON.stringify({ url: portalSession.url }),
      {
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
        status: 200,
      }
    )
  } catch (error) {
    console.error('Error:', error)
    return new Response(
      JSON.stringify({ error: error.message }),
      {
        headers: { ...corsHeaders, 'Content-Type': 'application/json' },
        status: 400,
      }
    )
  }
})
