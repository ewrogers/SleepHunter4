# History

As the version would suggest, this fourth major iteration 4 of SleepHunter.

Let's take a look back at how it came to be what it is today.

## Why is it called "SleepHunter"?

[Dark Ages](https://www.darkages.com) is a heavily in-character online role-playing game, especially in the earlier days.

Using some kind of program or "tool" to increase your skills was considered an unfair advantage and deemed against the rules.
In-character this was considered "sleephunting" (leveling your skills while being "asleep", or away from the keyboard).

If you were caught doing this, whether using a program or something as simple as a paperclip stuffed in your keyboard to auto-attack,
you would be punished for the crime of "sleephunting". This meant you would be temporarily banned from certain areas, or placed into a jail cell to serve time.

Your "legend", or history of character's deeds and misdeeds, would be tarnished with an orange mark for each time you were caught.
You could even have your character permanently banned if caught enough times.

So thus the name was born from being cheeky and naming an automation tool after the crime itself.

## Why did you make it?

In [Dark Ages](https://www.darkages.com) each skill or spell would start at level zero and gradually improve as you used it, usually up to the maximum of level 100.
Depending on the ability, the level would impact effectiveness or chance of success, or even duration.
To learn the next rank of an ability, or other higher abilities you often needed a certain level of an existing ability.

Being an online, social game it was very common for people to sit around non-hostile areas and improve their skill and spells levels while chatting.
Some people did it by hand, others used off-the-shelf "macro" prorgrams that would move their mouse cursor and type keys on the keyboard for them.
Usually, these were set up in somewhat a repeatable, looping script.

It was not considered "illegal" to do this if you were paying attention at the keyboard and could respond to an in-game "guard" or "Ranger" that suspected you.
However, if you did not respond, you would be in trouble and face punishment.

The main problem with existing tools of the time is that these macro programs would move your mouse cursor or send keystrokes to the current active application.
It made multi-tasking very difficult because your mouse would keep moving and keys would just be hit.

I wanted a specialized macro program that would send mouse and keyboard events only to the game client itself, allowing a seamless multi-tasking experience.
Fortunately, this was possible using [Win32 Window Messages](https://learn.microsoft.com/en-us/windows/win32/learnwin32/window-messages) and a debugging tool called [Spy++](https://learn.microsoft.com/en-us/visualstudio/debugger/introducing-spy-increment?view=vs-2022) from Microsoft.

## Why was "macroing" such a thing?

[Dark Ages](https://www.darkages.com) is a fairly long-running game initially released around August 1999.
It went through a long period of testing and design before that as well.

Being from that era, and based around "old school" D&D mechanics and table-top design it was what some would call very "grindy".
Meaning that it would take a lot of time for you to gain experience, level up, and progress your character.

During normal play of this time, your abilities would naturally develop as you hunted in groups and spent hours killing monsters.
So this was not nearly as much of an issue of abilities being "behind".

### Content Creep & Imbalance

However, as more content was added to the game, it became easier and easier to level up quite quickly.
To the point where you would always be very far behind and frustrated when going to learn the next ability.

In other cases, some vital skills would have abyssmal "hit" chances unless they were almost maxed.
Healing efficiency was also reduced and it was almost mana-inefficient at times unless you got to a certain level of your healing spells.

So to put it shortly, bad game design that was never revisited or fixed lead people to "macro" or "sleephunt" to get by.
This was a contentious issue that constantly went back and forth on the ethical debate. Some saying it was unfair play, others saying the development team should actively address the game balance.

### Enter the Dojo

At some point, a "sleephunting Dojo" was created by the development team. A place where you could pay gold and legally "macro" your skills while being away for a set amount of time (hours).
After which, it would automatically teleport you out and you would have to pay to re-enter again.

Ironically enough, if you got kicked out and continued to be "sleephunting", you could get in trouble as you were outside of the Dojo.

### SleepHunter v1 (2004)

The first version of SleepHunter came from my frustration with existing macro programs.
The main thing I wanted was the ability to multi-task properly while also improving my skills.

I was in college at that time and would come back home on the weekends to visit my parents.
I would use my dad's work laptop, some IBM Thinkpad and play Dark Ages while I was there.

I was still "learning" .NET (C#) at the time, so this program was written in Visual Basic 6 as I was more familiar with it.

The overall design was a listbox that you could add a variety of pre-defined commands, including repeat loops.
You could start/stop playback of the macro as well as save and load it for future use.

Despite being quite basic, it did use Window Messages to send events which meant that only the game client would receive those mouse and keyboard actions,
instead of the current application. It was now possible to "macro" or "sleephunt" while doing other things!

[PlanetDA link](https://www.planetda.net/index.php?/topic/10964-sleephunter-released-v1/)

### SleepHunter v2 (2004)

The second version was released only a month after the first, also written in Visual Basic 6.
At that time, it was obvious to me that I really wanted to expand the functionality possible with the application.

The major improvement in this version was reading character state from the game client itself,
enabling logic for if/else on variables like HP, MP, X, Y, and Map number.

There were also some changes in how it would activate the different skill/spell panels to (hopefully) keep the chat pane open
while you were macroing (if desired).

The overall application was more or less the same, just more game-specific commands that you could perform.

[PlanetDA link](https://www.planetda.net/index.php?/topic/11116-sleephunter-v2-released/)

### SleepHunter v3 (2005)

The third version was released the following year, completely rewritten in .NET 2.0.
I was enjoying C# compared to VB6 and it opened a lot of possibilities within the codebase.

There definitely was a learning curve, especially with Win32 interopability and P/Invokes but it was worth it.

Following the trend, more functions were added and multiple characters could be macroed at once.

This time the UI was split with the left sidebar showing the "library" of available functions,
and the right area hosted windows showing each character and their macro script.

It was amazing to see what people built using these commands, including "walking" scripts to far-off places.
Some would even "pathfind" by extensive if/else logic on the map coordinates.

[PlanetDA link](https://www.planetda.net/index.php?/topic/14097-sleephunter-v3-beta-121/)

### SleepHunter v4 (2012)

The fourth version was released several years, again rewritten in WPF instead of WinForms for the UI.

I was fascinated with the themeing and skinning potential of WPF, and went all out.
I probably spent more time on the UI design and XAML than I did actually coding the functionality.

I wanted to add more capabilities to make it the most efficient, yet easy to use macroing program for Dark Ages.
In order to do that, I knew I would need to be able to read more character state and implement the ability to
perform more actions like equipping items and having alternate characters assist others.

It became obvious to me that this was untennable from a "scripting" perspective, at least if the layperson was going to be able to use it.
So the user experience was drastically changed. Instead you could simply double-click to add spells and skills to a queue and be on your way.

The macro engine let you set a variety of parameters but for the most part it was automatic.
It could detect when to switch staves for better cast times, wait for mana regeneration, and even flower other characters for nearly-inifite mana.

All of these things required building XML data files so the application could understand the game mechanics.
To keep it sustainable, it is also user-editable within the application.

After looking back on it, it was worth the effort. The application aged incredibly well and stood the test of time,
even if being "ahead" of the modern flat design trend.

There was also a slight UI facelift in 2016 as well, mostly toning down font sizes and some iconography improvements.

[PlanetDA link](https://www.planetda.net/index.php?/topic/25089-sleephunter-v150-2016-facelift/)

## Going Forward (2023+)

While SleepHunter v4 continues to be the current version and fairly stable, there were always things I wanted to improve upon or add.
The infamous "Auto-Update" that never actually functioned is one of many (now available in v4.1.0).

A proper user manual hosted as this repositories GitHub Pages would also be useful, and can be linked from the application itself.

I also was never happy that despite using WPF, I ended up building it very WinForms-y in terms of code-behind instead of a proper MVVM data-binding application.
It's probably too much work to refactor for no apparent gain, but for anyone considering writing a WPF application today -- [use MVVM](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/).

There's still a few bugs I want to fix as well. I am still humbled by the fact people still use this application over two decades later.
That and I am surprised the game itself is still online, even if only a husk of its former self.

I absolutely accept PRs for this application! If you have a feature or bug fix that you want to implement yourself, have at it.
It should be much easier to coordinate and distribute now that `Auto-Update` is live.