# Stripe Payment Setup Guide

## Overview

Three subscription tiers:
- **Free**: $0 (no payment needed)
- **Premium**: $9.99/month
- **Creator**: $29.99/month

## Quick Start (Payment Links)

The fastest way to accept payments - no backend needed.

### 1. Create Stripe Account
1. Go to [stripe.com](https://stripe.com) and sign up
2. Complete account verification

### 2. Create Products in Stripe Dashboard

Go to **Products** → **Add Product**

#### Premium Tier
- **Name**: VR Street Art Gallery - Premium
- **Description**: Unlimited avatars, private rooms, early access
- **Pricing**: $9.99 / month (recurring)
- Save and copy the **Price ID** (starts with `price_`)

#### Creator Tier
- **Name**: VR Street Art Gallery - Creator
- **Description**: Host exhibitions, sell art, analytics dashboard
- **Pricing**: $29.99 / month (recurring)
- Save and copy the **Price ID** (starts with `price_`)

### 3. Create Payment Links

Go to **Payment Links** → **Create payment link**

For each product:
1. Select the product
2. **After payment**: Redirect to `https://hempp.github.io/street-art-gallery/success.html`
3. **Collect**: Email address (required)
4. Copy the payment link URL

### 4. Update Landing Page

Edit `index.html` and replace the waitlist buttons in the pricing section with your payment links:

```html
<!-- Premium tier button -->
<a href="https://buy.stripe.com/YOUR_PREMIUM_LINK" class="...">
    Subscribe - $9.99/mo
</a>

<!-- Creator tier button -->
<a href="https://buy.stripe.com/YOUR_CREATOR_LINK" class="...">
    Subscribe - $29.99/mo
</a>
```

---

## Advanced Setup (Stripe Checkout + Supabase)

For full control over the checkout flow and subscription management.

### Architecture

```
User clicks "Subscribe"
        ↓
Supabase Edge Function creates Checkout Session
        ↓
User completes payment on Stripe
        ↓
Stripe webhook → Supabase Edge Function
        ↓
Update user subscription in database
```

### 1. Get Stripe API Keys

Go to **Developers** → **API Keys**

Copy:
- **Publishable key**: `pk_test_...` or `pk_live_...`
- **Secret key**: `sk_test_...` or `sk_live_...`

### 2. Create Products via API

Run the setup script or use the Stripe Dashboard (see Quick Start).

### 3. Set Up Supabase Edge Functions

See `supabase/functions/` for:
- `create-checkout-session` - Creates Stripe Checkout
- `stripe-webhook` - Handles subscription events
- `customer-portal` - Manages existing subscriptions

### 4. Configure Webhooks

In Stripe Dashboard → **Developers** → **Webhooks**:

1. **Add endpoint**
2. **URL**: `https://YOUR_PROJECT.supabase.co/functions/v1/stripe-webhook`
3. **Events to listen**:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.paid`
   - `invoice.payment_failed`
4. Copy the **Webhook signing secret** (`whsec_...`)

### 5. Environment Variables

Add to Supabase → **Settings** → **Edge Functions** → **Secrets**:

```
STRIPE_SECRET_KEY=sk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...
STRIPE_PREMIUM_PRICE_ID=price_...
STRIPE_CREATOR_PRICE_ID=price_...
```

---

## Testing

### Test Mode
Use test API keys (`pk_test_`, `sk_test_`) for development.

### Test Card Numbers
- **Success**: `4242 4242 4242 4242`
- **Decline**: `4000 0000 0000 0002`
- **3D Secure**: `4000 0025 0000 3155`

Use any future expiry date and any 3-digit CVC.

### Test Webhooks Locally

```bash
# Install Stripe CLI
brew install stripe/stripe-cli/stripe

# Login
stripe login

# Forward webhooks to local function
stripe listen --forward-to localhost:54321/functions/v1/stripe-webhook
```

---

## Subscription States

| Status | Description | Access |
|--------|-------------|--------|
| `active` | Paid and current | Full tier access |
| `trialing` | In trial period | Full tier access |
| `past_due` | Payment failed, retrying | Limited access |
| `canceled` | User canceled | Downgrade to free |
| `unpaid` | Payment failed, exhausted retries | Downgrade to free |

---

## Customer Portal

Let users manage their own subscriptions:

```javascript
// Redirect to Stripe Customer Portal
const response = await fetch('/functions/v1/customer-portal', {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${session.access_token}` }
});
const { url } = await response.json();
window.location.href = url;
```

Users can:
- Update payment method
- View invoices
- Cancel subscription
- Change plan

---

## Revenue Reporting

### Stripe Dashboard
- **Home** → Revenue overview
- **Reports** → Detailed analytics
- **Billing** → Subscription metrics

### Key Metrics to Track
- Monthly Recurring Revenue (MRR)
- Churn rate
- Average Revenue Per User (ARPU)
- Conversion rate (free → paid)
