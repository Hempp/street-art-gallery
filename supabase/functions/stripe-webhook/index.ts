// Supabase Edge Function: Stripe Webhook Handler
// Deploy: supabase functions deploy stripe-webhook

import { serve } from 'https://deno.land/std@0.168.0/http/server.ts'
import { createClient } from 'https://esm.sh/@supabase/supabase-js@2'
import Stripe from 'https://esm.sh/stripe@13.10.0?target=deno'

const stripe = new Stripe(Deno.env.get('STRIPE_SECRET_KEY') as string, {
  apiVersion: '2023-10-16',
  httpClient: Stripe.createFetchHttpClient(),
})

const supabaseUrl = Deno.env.get('SUPABASE_URL') as string
const supabaseServiceKey = Deno.env.get('SUPABASE_SERVICE_ROLE_KEY') as string
const webhookSecret = Deno.env.get('STRIPE_WEBHOOK_SECRET') as string

// Price ID to tier mapping - UPDATE THESE WITH YOUR ACTUAL PRICE IDs
const PRICE_TO_TIER: Record<string, string> = {
  'price_premium_monthly': 'premium',    // Replace with actual price ID
  'price_creator_monthly': 'creator',    // Replace with actual price ID
  // Add yearly prices if needed
  'price_premium_yearly': 'premium',
  'price_creator_yearly': 'creator',
}

serve(async (req) => {
  const signature = req.headers.get('Stripe-Signature')
  if (!signature) {
    return new Response('No signature', { status: 400 })
  }

  try {
    const body = await req.text()

    // Verify webhook signature
    const event = stripe.webhooks.constructEvent(body, signature, webhookSecret)

    const supabase = createClient(supabaseUrl, supabaseServiceKey)

    console.log(`Processing event: ${event.type}`)

    switch (event.type) {
      case 'checkout.session.completed': {
        const session = event.data.object as Stripe.Checkout.Session

        if (session.mode === 'subscription' && session.subscription) {
          // Fetch the subscription details
          const subscription = await stripe.subscriptions.retrieve(
            session.subscription as string
          )
          await upsertSubscription(supabase, subscription)
        }
        break
      }

      case 'customer.subscription.created':
      case 'customer.subscription.updated': {
        const subscription = event.data.object as Stripe.Subscription
        await upsertSubscription(supabase, subscription)
        break
      }

      case 'customer.subscription.deleted': {
        const subscription = event.data.object as Stripe.Subscription
        await supabase
          .from('subscriptions')
          .update({
            status: 'canceled',
            canceled_at: new Date().toISOString(),
            updated_at: new Date().toISOString(),
          })
          .eq('id', subscription.id)
        break
      }

      case 'invoice.paid': {
        const invoice = event.data.object as Stripe.Invoice
        if (invoice.subscription) {
          const subscription = await stripe.subscriptions.retrieve(
            invoice.subscription as string
          )
          await upsertSubscription(supabase, subscription)
        }
        break
      }

      case 'invoice.payment_failed': {
        const invoice = event.data.object as Stripe.Invoice
        if (invoice.subscription) {
          await supabase
            .from('subscriptions')
            .update({
              status: 'past_due',
              updated_at: new Date().toISOString(),
            })
            .eq('id', invoice.subscription)
        }
        break
      }

      case 'customer.created':
      case 'customer.updated': {
        const customer = event.data.object as Stripe.Customer
        if (customer.metadata?.supabase_user_id) {
          await supabase
            .from('customers')
            .upsert({
              id: customer.metadata.supabase_user_id,
              stripe_customer_id: customer.id,
              email: customer.email,
              updated_at: new Date().toISOString(),
            })
        }
        break
      }

      default:
        console.log(`Unhandled event type: ${event.type}`)
    }

    return new Response(JSON.stringify({ received: true }), {
      headers: { 'Content-Type': 'application/json' },
      status: 200,
    })
  } catch (error) {
    console.error('Webhook error:', error)
    return new Response(
      JSON.stringify({ error: error.message }),
      { status: 400 }
    )
  }
})

// Helper function to upsert subscription
async function upsertSubscription(
  supabase: ReturnType<typeof createClient>,
  subscription: Stripe.Subscription
) {
  const priceId = subscription.items.data[0]?.price.id
  const tier = PRICE_TO_TIER[priceId] || 'premium'

  // Get user ID from subscription metadata or customer
  let userId = subscription.metadata?.supabase_user_id

  if (!userId) {
    // Try to get from customer
    const { data: customer } = await supabase
      .from('customers')
      .select('id')
      .eq('stripe_customer_id', subscription.customer)
      .single()

    userId = customer?.id
  }

  if (!userId) {
    console.error('No user ID found for subscription:', subscription.id)
    return
  }

  const subscriptionData = {
    id: subscription.id,
    user_id: userId,
    status: subscription.status,
    tier: tier,
    price_id: priceId,
    quantity: subscription.items.data[0]?.quantity || 1,
    cancel_at_period_end: subscription.cancel_at_period_end,
    cancel_at: subscription.cancel_at
      ? new Date(subscription.cancel_at * 1000).toISOString()
      : null,
    canceled_at: subscription.canceled_at
      ? new Date(subscription.canceled_at * 1000).toISOString()
      : null,
    current_period_start: new Date(
      subscription.current_period_start * 1000
    ).toISOString(),
    current_period_end: new Date(
      subscription.current_period_end * 1000
    ).toISOString(),
    trial_start: subscription.trial_start
      ? new Date(subscription.trial_start * 1000).toISOString()
      : null,
    trial_end: subscription.trial_end
      ? new Date(subscription.trial_end * 1000).toISOString()
      : null,
    updated_at: new Date().toISOString(),
    metadata: subscription.metadata || {},
  }

  const { error } = await supabase
    .from('subscriptions')
    .upsert(subscriptionData)

  if (error) {
    console.error('Error upserting subscription:', error)
    throw error
  }

  console.log(`Subscription ${subscription.id} upserted with tier: ${tier}`)
}
