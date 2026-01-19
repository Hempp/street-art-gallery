// Supabase Edge Function: Create Stripe Checkout Session
// Deploy: supabase functions deploy create-checkout-session

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

    // Parse request body
    const { priceId, successUrl, cancelUrl } = await req.json()

    if (!priceId) {
      throw new Error('Price ID is required')
    }

    // Get or create Stripe customer
    let customerId: string

    const { data: customer } = await supabase
      .from('customers')
      .select('stripe_customer_id')
      .eq('id', user.id)
      .single()

    if (customer?.stripe_customer_id) {
      customerId = customer.stripe_customer_id
    } else {
      // Create new Stripe customer
      const stripeCustomer = await stripe.customers.create({
        email: user.email,
        metadata: {
          supabase_user_id: user.id,
        },
      })
      customerId = stripeCustomer.id

      // Save to database
      await supabase
        .from('customers')
        .upsert({
          id: user.id,
          stripe_customer_id: customerId,
          email: user.email,
        })
    }

    // Create Checkout Session
    const session = await stripe.checkout.sessions.create({
      customer: customerId,
      line_items: [
        {
          price: priceId,
          quantity: 1,
        },
      ],
      mode: 'subscription',
      success_url: successUrl || `${req.headers.get('origin')}/success?session_id={CHECKOUT_SESSION_ID}`,
      cancel_url: cancelUrl || `${req.headers.get('origin')}/pricing`,
      subscription_data: {
        metadata: {
          supabase_user_id: user.id,
        },
      },
      allow_promotion_codes: true,
      billing_address_collection: 'auto',
    })

    return new Response(
      JSON.stringify({ sessionId: session.id, url: session.url }),
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
