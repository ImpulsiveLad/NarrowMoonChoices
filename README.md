![icon](https://github.com/ImpulsiveLad/NarrowMoonChoices/assets/92990441/866d4982-91b9-4e69-a8e9-6e2bb2e66e09)

Selene is the Greek Goddess of the moon.

## Selene's Choice keeps every run unique while also making the game more difficult

This is achieved by shuffling around the moons and only providing the crew with a free moon, paid moon, and a random one. (The # of each type to generate is configurable)

The ship will route to the first free moon it picks automatically.

All moons that were not picked by the shuffle will be hidden from the terminal and have their routes locked until the next shuffle.

## Shuffling Occurs when...

**A new lobby is opened** (The shuffle seed is based on the lobbyID)

**The crew is ejected** (The shuffle seed is the lobbyID plus the number of times the crew has been ejected)

Then either...

**Everyday** (The shuffle seed will be the previous level's seed)

Or...

**On a new Quota** (The shuffle will be the lobbyID plus the new quota amount)

By default the shuffle will occur daily, this can be configured.

Moons that are recently selected by the shuffle will be excluded for a few days.
Default is only the 'safety moon' and for 3 days, it can also temp block all moons from the shuffle and the # of days to hold them is configurable. (Feature can be disabled alltogether as well as some other config settings for it)

You are also able to create an Ignorelist, Blacklist and Treasurelist. All are exempt from the shuffle but:
- Ignored moons are will always be visible and unlocked.
- Blacklisted moons will always be invisible and locked.
- Treasured moons will always be invisible but unlocked, there is config to make them give extra scrap if desired.
- You can choose whether the secret unlockable Rosie's moon are to be untouched or not.

Additional config options include for the guaranteed free moon to always have clear weather, a discount on selected moons' route prices (min/max configurable), and rolling over the paid moon count to the random count when there are no paid moons left (default:true).

## This mod is **highly** dependent on LLL's functions and moon properties so any moons that don't use LLL will most likely not work with this mod.

All players require the mod.

If you find any bugs it will be faster to tell me on discord tbh.



*Tips are always appreciated* <3 <3 <3 <3 <3
https://ko-fi.com/impulsivelass
