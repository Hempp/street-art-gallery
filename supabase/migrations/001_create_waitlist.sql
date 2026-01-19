-- ============================================
-- VR Street Art Gallery - Waitlist Table
-- ============================================

-- Create waitlist table
CREATE TABLE IF NOT EXISTS waitlist (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    email TEXT NOT NULL UNIQUE,
    source TEXT DEFAULT 'website',
    tier_interest TEXT DEFAULT 'free',
    referrer TEXT,
    user_agent TEXT,
    ip_country TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    email_verified BOOLEAN DEFAULT FALSE,
    subscribed BOOLEAN DEFAULT TRUE
);

-- Create index for faster email lookups
CREATE INDEX IF NOT EXISTS idx_waitlist_email ON waitlist(email);
CREATE INDEX IF NOT EXISTS idx_waitlist_created_at ON waitlist(created_at DESC);

-- Enable Row Level Security
ALTER TABLE waitlist ENABLE ROW LEVEL SECURITY;

-- Policy: Allow anonymous inserts (for waitlist signups)
CREATE POLICY "Allow public insert" ON waitlist
    FOR INSERT
    TO anon
    WITH CHECK (true);

-- Policy: Only authenticated users can read (for admin)
CREATE POLICY "Allow authenticated read" ON waitlist
    FOR SELECT
    TO authenticated
    USING (true);

-- Function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to auto-update updated_at
CREATE TRIGGER update_waitlist_updated_at
    BEFORE UPDATE ON waitlist
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Function to get waitlist count (public)
CREATE OR REPLACE FUNCTION get_waitlist_count()
RETURNS INTEGER AS $$
BEGIN
    RETURN (SELECT COUNT(*) FROM waitlist WHERE subscribed = true);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Grant execute permission to anon for count function
GRANT EXECUTE ON FUNCTION get_waitlist_count() TO anon;

-- ============================================
-- Analytics Views (for admin dashboard)
-- ============================================

-- Daily signups view
CREATE OR REPLACE VIEW waitlist_daily_stats AS
SELECT
    DATE(created_at) as signup_date,
    COUNT(*) as signups,
    COUNT(CASE WHEN tier_interest = 'premium' THEN 1 END) as premium_interest,
    COUNT(CASE WHEN tier_interest = 'creator' THEN 1 END) as creator_interest
FROM waitlist
WHERE subscribed = true
GROUP BY DATE(created_at)
ORDER BY signup_date DESC;

-- Source breakdown view
CREATE OR REPLACE VIEW waitlist_source_stats AS
SELECT
    source,
    COUNT(*) as total,
    ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM waitlist), 2) as percentage
FROM waitlist
WHERE subscribed = true
GROUP BY source
ORDER BY total DESC;
