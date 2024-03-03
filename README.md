CustomDeathPenalty is a simple little mod that adds some config values to control the fines for dead coworkers and a new mechanic for increasing the current quota based on player deaths.

## There are 4 config values for fines:
- Fine % on any moon (other than the company)
- Fine reduction for any moon (other than the company)
- Fine % on the company moon
- Fine reduction on the company moon

The config values for non-company moons is the vanilla value at default.

The config values for the company is no penalty by default.

## The mod also adds a new mechanic where each unrecovered player at the end of the round increases the quota.
The % added per body is configurable.

The default config is 10% extra quota per unrecovered body and this mechanic does not occur on the company moon.

You could instead increase the quota by a set amount (100% by default) when all players are dead. If some survive it will be that percent times the unrecovered players over the total players.

## The mod includes an experimental feature that makes the scrap value on the moon scale based on the quota and enemy power of the moon
Having this enabled may make the game overall more difficult but it allows you to reach previously unobtainable quotas (If you reach 2,147,483,647 then you win :3 !!)

The feature is disabled by default, if you choose to enable it then make sure to adjust the Offset value below it accordingly.

There are several config values to tweak the feature to your liking

The easiest I can explain the math behind the calculation is ScrapValue = (Q*M*E)+O
Where:
Q = current Quota
M = config for overall min and max multiplier"
E = current moon enemy power level divided by the threshold config plus one
O = offset in config

## Another experimental feature calculates the multiplier for the interior size based on the quota and enemy power on the moon
This featured changes the interior size multiplier that is typically locked per moon, 1x on Experimentation and 2.35x on Titan for example.

This new value gets the ScrapValue on the moon and for every x scrap it will increase the the multiplier by 0.01.

There are four configurable values attached to the feature that allow you to edit it to your liking.

Keep in mind that interior mods use their own min/max multiplier so you must change them in their config in order for the min/max clamp you set in this config to work.

## The mod also makes the end round screen display a more accurate total scrap in level count (merged from another mod of mine)
