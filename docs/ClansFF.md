
# ClansFF
Provides the option to remove friendly fire for your clan mates!

## Permission
Players must have the *clansff.use* permission to use clan friendly fire features

## Required Plugins
* [Clans](https://umod.org/plugins/clans)

## Usage
**/cff** OR **/cff help** - Displays information about the plugin, as well as the current clan friendly fire status.
**/cff on | off** - Toggles clan friendly fire on or off.  Only usable by the clan leader.

## API
* bool ShouldDamageClanFF(BasePlayer player1, BasePlayer player2)
	* Returns false if both players are in the same clan and should not damage each other based on friendly fire status