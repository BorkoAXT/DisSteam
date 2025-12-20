# Steam ↔ Discord Integration App

A Discord **user-installable app** that links Steam accounts to Discord users and exposes rich, privacy-aware Steam data directly through Discord slash commands — globally, without requiring server-specific setup.

This project focuses on **verified identity**, **game activity**, and **useful insights**, not simple chat mirroring.

---

## Features Overview

- One-time Steam account linking (global, reusable everywhere)
- Works via Discord slash commands (`/`) in any server or DM
- No manual SteamID input required
- Privacy-respecting (only public Steam data is shown)
- Rich embeds designed for clean, readable output
- Built to scale with caching and rate-limit awareness

---

## Account Linking (Required)

### Goal
Map a Discord user to a Steam account securely and permanently.

### What’s Linked
- SteamID64 (unique, immutable identifier)
- Vanity URL → SteamID64 resolution (optional)
- Proof of ownership via Steam OpenID sign-in

### How It Works
1. User runs `/steam link`
2. User authenticates with Steam OpenID
3. SteamID64 is returned and verified
4. Mapping is stored globally:

This link works across all Discord servers where the app is used.

---

## Public Profile & Presence

### Extractable Data
- Persona name (display name)
- Profile URL
- Avatar (small / medium / full)
- Profile & community visibility state
- Persona state (offline / online / away / snooze)
- Currently in-game app ID + game name (if applicable)
- Last logoff timestamp
- Account creation time (if available)
- Country code (if present)

### Discord Usage
- Steam profile buttons
- Presence-based indicators (online / in-game)

---

## Steam Level, Badges & XP

### Extractable Data
- Steam level
- Badge list:
  - Badge ID
  - Badge level
  - Badge XP
  - Completion timestamp (if available)
- Total badge count (computed)
- Total XP (computed)
- Community badge progress

---

## Bans & Trust Signals

### Extractable Data
- VAC ban status
- Number of VAC bans
- Number of game bans
- Days since last ban
- Community ban status
- Economy ban status

### Common Uses
- Trust indicators (e.g. Trusted Trader)
- Server moderation policies (opt-in)

---

## Friends & Social Graph (Public Only)

### Extractable Data
- Friends list (SteamIDs)
- Friend since timestamp (when available)
- Steam group memberships

---

## Owned Games & Library Summary (Public Only)

### Extractable Data
- Owned games list (app IDs)
- Total owned games (computed)
- Playtime per game (minutes)
- Playtime in the last 2 weeks
- Recently played games
- Top N games by total playtime
- Total library playtime (computed)

---

## Per-Game Achievements & Stats

### Extractable Per Game
- Achievements:
  - Achievement ID & name
  - Unlocked status
  - Unlock timestamp (if available)
  - Completion percentage (computed)
- Game stats:
  - Any numeric stats exposed by the game

> Note: Availability varies by game. Not all titles expose stats or achievements.

---

## Inventory Data (Public Inventories Only)

### Inventory Listing
- Asset ID
- Class ID / Instance ID
- Quantity
- Tradable / marketable flags
- Item descriptions:
  - Name
  - Type
  - Rarity / quality tags
  - Lore / descriptions
  - Icon URLs

### Inventory Worth (Estimated)
- Total item count
- Counts by rarity / category
- Estimated market value:
  - Per-item price lookup
  - Total value sum
  - Tradable vs non-tradable value

### Caveats
- Steam does not provide a direct inventory value
- Prices vary by market and currency
- Some items have no market data
- Rate limiting requires caching

---

## Trading & Market Signals (Limited)

### Possible (Inferred)
- Tradable / marketable item counts
- Trade-hold indicators (per item, when inferable)

### Not Available
- Trade history
- Active trade offers
- Wallet balance
- Purchase history

---

## Game Metadata (Contextual)

- App details (name, icon)
- Current player counts
- Game news posts

---

## Data That Is Not Accessible

- Private profile data
- Private inventories, friends, or games
- Wishlist or purchase history
- Email or real name
- Anything requiring Steam passwords or cookies

---

## Everything Card (Discord Embed Layout)

For each linked user:
1. Profile – name, avatar, status, profile link
2. Progression – Steam level, badges, total XP
3. Trust – VAC / game / community ban summary
4. Games – owned count, recently played, top playtime
5. Game Panel – achievements and stats (1–3 supported games)
6. Inventory – item count, tradable count, estimated value

---

## Tech Stack

### Discord
- Discord Application Commands (slash commands)
- User-installable app (global usage)
- Rich embeds and interactive components

### Steam
- Steam OpenID authentication
- Steam Web API
- Steam Community inventory endpoints (public only)

### Backend
- REST API service
- Database for global account linking
- Caching layer to handle rate limits

### Security & Privacy
- No Steam credentials stored
- Opt-in account linking
- Public data only
- Global unlink support

---

## How It Works (High Level)

---

## Notes

- All Steam data respects user privacy settings
- Inventory value is always an estimate
- Feature availability depends on game support
- Designed to scale for large communities