# Supabase Authentication Setup Guide

Complete guide for setting up authentication in the VR Street Art Gallery.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Supabase Configuration](#supabase-configuration)
3. [Database Setup](#database-setup)
4. [Authentication Providers](#authentication-providers)
5. [Frontend Configuration](#frontend-configuration)
6. [Testing](#testing)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- Supabase project created at [supabase.com](https://supabase.com)
- Stripe account configured (see STRIPE_SETUP.md)
- Node.js 18+ for local development

---

## Supabase Configuration

### 1. Get Your Credentials

From your Supabase dashboard:

1. Go to **Settings** > **API**
2. Copy the following values:
   - **Project URL** (e.g., `https://xxx.supabase.co`)
   - **anon/public key** (starts with `eyJ...`)

### 2. Update Frontend Code

In `index.html`, replace the placeholder values:

```javascript
const SUPABASE_URL = 'https://your-project-ref.supabase.co';
const SUPABASE_ANON_KEY = 'your-anon-key';
```

Also update `success.html` with the same values.

---

## Database Setup

### Run Migrations

Execute the migrations in order:

```bash
# Using Supabase CLI
supabase db push

# Or run manually in SQL Editor (Dashboard > SQL Editor)
```

**Migration files:**
1. `001_create_customers.sql` - Stripe customer mapping
2. `002_create_subscriptions.sql` - Subscription management
3. `003_create_profiles.sql` - User profiles with auth integration

### Key Tables

#### `profiles` Table
Extends Supabase auth with app-specific data:

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | References auth.users(id) |
| email | TEXT | User email |
| full_name | TEXT | Full name |
| display_name | TEXT | Display name |
| avatar_url | TEXT | Profile picture URL |
| bio | TEXT | User biography |
| tier | TEXT | Subscription tier (free/premium/creator) |
| preferences | JSONB | User preferences |
| website | TEXT | Personal website |
| twitter | TEXT | Twitter handle |
| instagram | TEXT | Instagram handle |

### Automatic Profile Creation

A trigger automatically creates a profile when a user signs up:

```sql
-- Trigger runs on auth.users insert
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();
```

### Tier Sync

Subscription changes automatically update the user's tier:

```sql
-- Trigger runs on subscriptions insert/update
CREATE TRIGGER on_subscription_change
    AFTER INSERT OR UPDATE ON public.subscriptions
    FOR EACH ROW EXECUTE FUNCTION public.sync_subscription_tier();
```

---

## Authentication Providers

### Email/Password (Default)

Email authentication is enabled by default. Configure settings in:

**Dashboard** > **Authentication** > **Providers** > **Email**

Recommended settings:
- [x] Enable Email Signup
- [x] Enable Email Confirmations (production)
- [ ] Disable Email Confirmations (development)
- [x] Secure Password Change

### Google OAuth

1. Go to [Google Cloud Console](https://console.cloud.google.com)
2. Create a new project or select existing
3. Enable **Google+ API**
4. Go to **Credentials** > **Create Credentials** > **OAuth Client ID**
5. Configure consent screen
6. Add authorized redirect URI:
   ```
   https://your-project-ref.supabase.co/auth/v1/callback
   ```
7. Copy **Client ID** and **Client Secret**
8. In Supabase Dashboard:
   - Go to **Authentication** > **Providers** > **Google**
   - Enable and paste credentials

### Discord OAuth

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application
3. Go to **OAuth2** > **General**
4. Add redirect URI:
   ```
   https://your-project-ref.supabase.co/auth/v1/callback
   ```
5. Copy **Client ID** and **Client Secret**
6. In Supabase Dashboard:
   - Go to **Authentication** > **Providers** > **Discord**
   - Enable and paste credentials

### Additional Providers

Supabase supports many OAuth providers. Popular options for VR/gaming:
- **Twitch** - Gaming community
- **Steam** - Gaming platform
- **Apple** - iOS users

---

## Frontend Configuration

### Auth Flow

The authentication flow is handled in `index.html`:

1. **Sign Up**
   ```javascript
   const { data, error } = await supabase.auth.signUp({
       email: email,
       password: password,
       options: {
           data: {
               full_name: name
           }
       }
   });
   ```

2. **Sign In**
   ```javascript
   const { data, error } = await supabase.auth.signInWithPassword({
       email: email,
       password: password
   });
   ```

3. **Social Login**
   ```javascript
   const { data, error } = await supabase.auth.signInWithOAuth({
       provider: 'google', // or 'discord'
       options: {
           redirectTo: window.location.origin
       }
   });
   ```

4. **Sign Out**
   ```javascript
   await supabase.auth.signOut();
   ```

### UI Components

| Component | ID | Purpose |
|-----------|-----|---------|
| Auth Buttons | `#auth-buttons` | Login/signup buttons (logged out) |
| User Menu | `#user-menu` | User dropdown (logged in) |
| Auth Modal | `#auth-modal` | Login/signup forms |
| User Avatar | `#user-avatar` | Profile picture display |
| User Name | `#user-name` | Display name |
| User Tier | `#user-tier` | Subscription badge |

### Auth State Management

```javascript
// Initialize auth on page load
document.addEventListener('DOMContentLoaded', initAuth);

// Listen for auth changes
supabase.auth.onAuthStateChange((event, session) => {
    handleAuthStateChange(event, session);
});
```

---

## Testing

### Local Development

1. Disable email confirmation for easier testing:
   - Dashboard > Authentication > Providers > Email
   - Uncheck "Enable email confirmations"

2. Test user accounts:
   ```
   test@example.com / TestPassword123!
   premium@example.com / TestPassword123!
   ```

### Test Social Login

1. Enable "Redirect URLs" in Supabase:
   - Dashboard > Authentication > URL Configuration
   - Add `http://localhost:3000` to allowed redirect URLs

2. For production, add your domain:
   ```
   https://yourdomain.com
   https://hempp.github.io
   ```

### Verify Profile Creation

After signup, check the profiles table:

```sql
SELECT * FROM profiles WHERE email = 'test@example.com';
```

---

## Troubleshooting

### Common Issues

#### "Invalid login credentials"
- Check email/password are correct
- Verify email confirmation if required
- Check Supabase logs for details

#### "Email not confirmed"
- User needs to click confirmation link
- Or disable email confirmations for testing

#### Social login redirects to blank page
- Check redirect URLs in both:
  - OAuth provider settings
  - Supabase URL Configuration

#### Profile not created on signup
- Verify trigger is installed:
  ```sql
  SELECT * FROM pg_trigger WHERE tgname = 'on_auth_user_created';
  ```
- Check Supabase logs for trigger errors

#### Tier not syncing
- Verify subscription webhook is working
- Check sync_subscription_tier trigger exists
- Manual fix:
  ```sql
  UPDATE profiles SET tier = 'premium' WHERE id = 'user-uuid';
  ```

### Debug Mode

Enable console logging for auth debugging:

```javascript
// Add to initAuth()
console.log('Auth initialized');
supabase.auth.onAuthStateChange((event, session) => {
    console.log('Auth event:', event);
    console.log('Session:', session);
});
```

### Supabase Logs

View authentication logs:
- Dashboard > Logs > Auth

---

## Security Checklist

- [ ] Enable email confirmations in production
- [ ] Configure proper redirect URLs
- [ ] Remove test accounts before launch
- [ ] Enable RLS on all tables
- [ ] Review OAuth scopes (minimal permissions)
- [ ] Set up rate limiting
- [ ] Configure password requirements

---

## Environment Variables

For deployment, set these environment variables:

```bash
# Supabase
SUPABASE_URL=https://xxx.supabase.co
SUPABASE_ANON_KEY=eyJ...

# OAuth (in Supabase Dashboard)
GOOGLE_CLIENT_ID=xxx
GOOGLE_CLIENT_SECRET=xxx
DISCORD_CLIENT_ID=xxx
DISCORD_CLIENT_SECRET=xxx
```

---

## Related Documentation

- [Supabase Auth Docs](https://supabase.com/docs/guides/auth)
- [OAuth Providers](https://supabase.com/docs/guides/auth/social-login)
- [Row Level Security](https://supabase.com/docs/guides/auth/row-level-security)
- [STRIPE_SETUP.md](./STRIPE_SETUP.md) - Payment integration

---

*VR Street Art Gallery - Authentication System v1.0*
