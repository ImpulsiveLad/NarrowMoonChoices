![icon](https://github.com/ImpulsiveLad/NarrowMoonChoices/assets/92990441/866d4982-91b9-4e69-a8e9-6e2bb2e66e09)

Selene is the Greek Goddess of the moon.

# *Let her guide your crew*

Selene's Choice is a Lethal Company mod oriented around unpredictability and inadvertent difficulty.

This is achieved by shuffling moons and only providing the crew access to a limited array of moons. All other moons will be hidden and locked.

The ship auto-routes to one of these moons automatically. If the quota is due, the ship auto-routes to the company instead.

The shuffle can either occur every day or only after a quota has been passed based on a setting. (default: daily)

# Valid Moons

The config allows you to choose how many moons to select based on their route price:

- **Free**: Route price of 0. (default: 1)
- **Paid**: Route price of more than 0. (default: 1)
- **Extra**: Any route price. (default: 1)
- **Rare**: Route price higher than a configurable threshold value. (default: 0, threshold of 650c)

If there aren't enough moons in a category to meet the requested count, the remaining count can roll over into the extra moons category. (default: true)

# Other Settings

- Should the auto-route moon always have clear weather? (default: false)
  - This works with WeatherRegistry as well.

- Should the ship not auto-route on rejoining an old save? (default: false)
  - This can prevent leaving a paid moon you have if you don't have permanent moons installed but causes some weird behavior like being on the moon still when its not in the new shuffle.

- Should the shuffle exclude moons that were recently selected? (default: true)
  - Should it only remember the auto-route moon or all moons in the shuffle? (default: only auto-route moon)
  - How many days should the shuffle exclude these moons for? (default: 3 days)
  - If there are no valid moons left, should it return just one or all? (default: just one moon)
  - Should it start returning moons when there are no free moons left or not return moons until all moons are unavailable? (default: when no moons are available)

- Discount for selected moons. The minimum and maximum price reduction percent are configurable. (default: false, 40-60% discount)

# Moon Lists

- **Ignore List**: Always available and excluded from the shuffle. (default: Gordion)
- **Blacklist**: Always locked, hidden, and excluded from the shuffle. (default: Liquidation)
- **Treasure List**: Always available but hidden, excluded from the shuffle. (default: Embrion)
  - Multiplier for the scrap count on Treasure Moons. (default: 1.25 times more scrap)

## Notes

All players require the mod.

This mod is *highly* dependent on LethalLeveLoader functions and moon properties so any moons that are not implemented using LLL will most likely not work with this mod.

Csync should sync everyone's config settings to the host, any other important data is sent via named message.

If you find any bugs it will be faster to tell me on discord tbh.



*Tips are always appreciated* <3 <3 <3 <3 <3
https://ko-fi.com/impulsivelass
