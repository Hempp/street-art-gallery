-- ============================================
-- VR Street Art Gallery - Subscriptions & Customers
-- ============================================

-- Create customers table (links Supabase auth to Stripe)
CREATE TABLE IF NOT EXISTS customers (
    id UUID REFERENCES auth.users(id) PRIMARY KEY,
    stripe_customer_id TEXT UNIQUE,
    email TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create subscriptions table
CREATE TABLE IF NOT EXISTS subscriptions (
    id TEXT PRIMARY KEY, -- Stripe subscription ID
    user_id UUID REFERENCES auth.users(id) NOT NULL,
    status TEXT NOT NULL, -- active, canceled, past_due, trialing, unpaid
    tier TEXT NOT NULL, -- free, premium, creator
    price_id TEXT, -- Stripe price ID
    quantity INTEGER DEFAULT 1,
    cancel_at_period_end BOOLEAN DEFAULT FALSE,
    cancel_at TIMESTAMPTZ,
    canceled_at TIMESTAMPTZ,
    current_period_start TIMESTAMPTZ,
    current_period_end TIMESTAMPTZ,
    trial_start TIMESTAMPTZ,
    trial_end TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    metadata JSONB DEFAULT '{}'::jsonb
);

-- Create prices table (cache of Stripe prices)
CREATE TABLE IF NOT EXISTS prices (
    id TEXT PRIMARY KEY, -- Stripe price ID
    product_id TEXT NOT NULL,
    active BOOLEAN DEFAULT TRUE,
    currency TEXT DEFAULT 'usd',
    unit_amount INTEGER, -- Amount in cents
    interval TEXT, -- month, year
    interval_count INTEGER DEFAULT 1,
    trial_period_days INTEGER,
    tier TEXT, -- premium, creator
    metadata JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create products table (cache of Stripe products)
CREATE TABLE IF NOT EXISTS products (
    id TEXT PRIMARY KEY, -- Stripe product ID
    active BOOLEAN DEFAULT TRUE,
    name TEXT,
    description TEXT,
    image TEXT,
    metadata JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_subscriptions_user_id ON subscriptions(user_id);
CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON subscriptions(status);
CREATE INDEX IF NOT EXISTS idx_customers_stripe_id ON customers(stripe_customer_id);

-- Enable RLS
ALTER TABLE customers ENABLE ROW LEVEL SECURITY;
ALTER TABLE subscriptions ENABLE ROW LEVEL SECURITY;
ALTER TABLE prices ENABLE ROW LEVEL SECURITY;
ALTER TABLE products ENABLE ROW LEVEL SECURITY;

-- Customers policies
CREATE POLICY "Users can view own customer data" ON customers
    FOR SELECT USING (auth.uid() = id);

CREATE POLICY "Service role can manage customers" ON customers
    FOR ALL USING (auth.role() = 'service_role');

-- Subscriptions policies
CREATE POLICY "Users can view own subscriptions" ON subscriptions
    FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Service role can manage subscriptions" ON subscriptions
    FOR ALL USING (auth.role() = 'service_role');

-- Prices policies (public read)
CREATE POLICY "Anyone can view active prices" ON prices
    FOR SELECT USING (active = true);

-- Products policies (public read)
CREATE POLICY "Anyone can view active products" ON products
    FOR SELECT USING (active = true);

-- ============================================
-- Helper Functions
-- ============================================

-- Get user's current subscription tier
CREATE OR REPLACE FUNCTION get_user_tier(user_uuid UUID)
RETURNS TEXT AS $$
DECLARE
    user_tier TEXT;
BEGIN
    SELECT tier INTO user_tier
    FROM subscriptions
    WHERE user_id = user_uuid
      AND status IN ('active', 'trialing')
    ORDER BY created_at DESC
    LIMIT 1;

    RETURN COALESCE(user_tier, 'free');
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Check if user has active subscription
CREATE OR REPLACE FUNCTION has_active_subscription(user_uuid UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1 FROM subscriptions
        WHERE user_id = user_uuid
          AND status IN ('active', 'trialing')
    );
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Get subscription details for current user
CREATE OR REPLACE FUNCTION get_my_subscription()
RETURNS TABLE (
    subscription_id TEXT,
    tier TEXT,
    status TEXT,
    current_period_end TIMESTAMPTZ,
    cancel_at_period_end BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        s.id,
        s.tier,
        s.status,
        s.current_period_end,
        s.cancel_at_period_end
    FROM subscriptions s
    WHERE s.user_id = auth.uid()
      AND s.status IN ('active', 'trialing', 'past_due')
    ORDER BY s.created_at DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================
-- Triggers
-- ============================================

-- Update timestamp trigger for customers
CREATE TRIGGER update_customers_updated_at
    BEFORE UPDATE ON customers
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Update timestamp trigger for subscriptions
CREATE TRIGGER update_subscriptions_updated_at
    BEFORE UPDATE ON subscriptions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- Insert default products and prices
-- ============================================

-- Premium product
INSERT INTO products (id, active, name, description, metadata)
VALUES (
    'prod_premium',
    true,
    'VR Street Art Gallery - Premium',
    'Unlimited avatars, private rooms, early access exhibitions, virtual art collecting',
    '{"tier": "premium"}'::jsonb
) ON CONFLICT (id) DO NOTHING;

-- Creator product
INSERT INTO products (id, active, name, description, metadata)
VALUES (
    'prod_creator',
    true,
    'VR Street Art Gallery - Creator',
    'Everything in Premium + host exhibitions, sell virtual art, analytics dashboard, custom gallery spaces',
    '{"tier": "creator"}'::jsonb
) ON CONFLICT (id) DO NOTHING;

-- Premium price (placeholder - update with real Stripe price ID)
INSERT INTO prices (id, product_id, active, currency, unit_amount, interval, tier, metadata)
VALUES (
    'price_premium_monthly',
    'prod_premium',
    true,
    'usd',
    999, -- $9.99 in cents
    'month',
    'premium',
    '{"tier": "premium"}'::jsonb
) ON CONFLICT (id) DO NOTHING;

-- Creator price (placeholder - update with real Stripe price ID)
INSERT INTO prices (id, product_id, active, currency, unit_amount, interval, tier, metadata)
VALUES (
    'price_creator_monthly',
    'prod_creator',
    true,
    'usd',
    2999, -- $29.99 in cents
    'month',
    'creator',
    '{"tier": "creator"}'::jsonb
) ON CONFLICT (id) DO NOTHING;
