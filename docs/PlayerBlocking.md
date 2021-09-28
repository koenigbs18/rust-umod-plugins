
# PlayerBlocking
Adds raid blocking and combat blocking to prevent players from using certain plugins.

## Permission
Players must have the *playerblocking.use* permission to be affected by combat/raid blocking
Admins may override this behavior by being granted the *playerblocking.admin* permission

## Required Plugins
* [Clans](https://umod.org/plugins/clans)
* [ImageLibrary](https://umod.org/plugins/image-library)

## Config
1. "Minimum Combat Block Damage" - float; minimum damage to a player before being combat blocked.  Useful for stopping bleeding from causing combat block
2. "Combat Block Duration (in seconds)" - integer
3. "Combat Damage Types" - list of DamageType strings; if the source of damage is not in this list, then combat block will not applied
4. "Minimum Raid Block Damage" - float; how much damage must be done to a construction block in order to cause raid blocking
5. "Raid Block Duration (in seconds)" - integer
6. "Raid Block Distance" - float; raid blocks all players within this distance from the source of damage
7. "Raid Damage Types" - list of DamageType strings; if the source of damage is not in this list, then raid block will not be applied

## Plugins Blocked
Combat Block
* /trade
* /tp

Raid Block
* /trade
* /tp
* /remove