# Supabase Setup Guide

## Quick Start

### 1. Create a Supabase Project

1. Go to [supabase.com](https://supabase.com) and sign up/login
2. Click **New Project**
3. Enter project details:
   - **Name**: `vr-street-art-gallery`
   - **Database Password**: (save this securely)
   - **Region**: Choose closest to your users
4. Click **Create new project** and wait for setup (~2 mins)

### 2. Run the Migration

1. In your Supabase dashboard, go to **SQL Editor**
2. Click **New query**
3. Copy and paste the contents of `migrations/001_create_waitlist.sql`
4. Click **Run** (or Cmd/Ctrl + Enter)

You should see: `Success. No rows returned`

### 3. Get Your API Keys

1. Go to **Settings** → **API**
2. Copy these values:
   - **Project URL**: `https://xxxxx.supabase.co`
   - **anon public key**: `eyJhbGciOiJIUzI1NiIs...`

### 4. Update the Landing Page

Edit `index.html` and replace the placeholder values:

```javascript
const SUPABASE_URL = 'https://YOUR_PROJECT_ID.supabase.co';
const SUPABASE_ANON_KEY = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';
```

### 5. Test the Waitlist

1. Open the landing page
2. Enter a test email
3. Check Supabase **Table Editor** → **waitlist** to see the entry

---

## Database Schema

### `waitlist` Table

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `email` | TEXT | User email (unique) |
| `source` | TEXT | Where they signed up (website, social, etc.) |
| `tier_interest` | TEXT | Which pricing tier they clicked (free, premium, creator) |
| `referrer` | TEXT | HTTP referrer |
| `user_agent` | TEXT | Browser info |
| `ip_country` | TEXT | Country (requires edge function) |
| `created_at` | TIMESTAMP | Signup time |
| `email_verified` | BOOLEAN | Email verification status |
| `subscribed` | BOOLEAN | Still subscribed to updates |

---

## Security (Row Level Security)

The migration includes these RLS policies:

- **Public Insert**: Anyone can add their email (waitlist signup)
- **Authenticated Read**: Only logged-in admins can view the list

---

## Viewing Waitlist Data

### Option 1: Supabase Dashboard
1. Go to **Table Editor** → **waitlist**
2. See all signups with filtering/sorting

### Option 2: SQL Query
```sql
-- Total signups
SELECT COUNT(*) FROM waitlist;

-- Recent signups
SELECT email, tier_interest, created_at
FROM waitlist
ORDER BY created_at DESC
LIMIT 20;

-- Daily stats
SELECT * FROM waitlist_daily_stats;

-- Source breakdown
SELECT * FROM waitlist_source_stats;
```

### Option 3: Export to CSV
1. Go to **Table Editor** → **waitlist**
2. Click **Export** → **Download as CSV**

---

## Optional: Email Notifications

To get notified of new signups, create a database webhook:

1. Go to **Database** → **Webhooks**
2. Click **Create webhook**
3. Configure:
   - **Name**: `new_waitlist_signup`
   - **Table**: `waitlist`
   - **Events**: `INSERT`
   - **URL**: Your webhook endpoint (Slack, Discord, email service, etc.)

### Slack Integration Example

Use a Slack webhook URL to post new signups to a channel:

```
https://hooks.slack.com/services/YOUR/SLACK/WEBHOOK
```

---

## Troubleshooting

### "Permission denied" error
- Check RLS policies are created correctly
- Verify you're using the `anon` key (not `service_role`)

### Duplicate email error
- This is expected behavior - the table has a unique constraint
- The frontend handles this with a friendly message

### Count not updating
- Clear browser cache
- Check browser console for errors
- Verify the `get_waitlist_count` function exists

---

## Environment Variables (for production)

Instead of hardcoding keys, use environment variables:

```javascript
const SUPABASE_URL = import.meta.env.VITE_SUPABASE_URL;
const SUPABASE_ANON_KEY = import.meta.env.VITE_SUPABASE_ANON_KEY;
```

For GitHub Pages (static site), you'll need to:
1. Use a build step (Vite, Next.js, etc.)
2. Or use a serverless function to proxy requests
3. Or accept that anon keys are public (they're designed to be)

The `anon` key is safe to expose - RLS policies protect your data.
