# Introduction

## What is SleepHunter?

SleepHunter is an automation tool for [Dark Ages](https://www.darkages.com), a massively multiplayer online role-playing game (MMORPG).
It is used to automate the repetitive process of leveling a character's abilities.

The current version is **4.x**, which is the culmination of over a decade of experience, testing, and enhancements from user feedback.

You can view the [release notes](./CHANGELOG.md) to see what has changed in each version.

![image](./screenshots/SleepHunter.png)

## What was it made using?

SleepHunter is written in C# using the [Windows Presentation Foundation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netdesktop-7.0) (WPF) framework.

It is built using the [Visual Studio](https://visualstudio.microsoft.com/) IDE.

The user interface is designed using custom XAML styles and templates.

You can view the source code on [GitHub](https://www.github.com/ewrogers/SleepHunter4).

#### Why didn't you go network (packet-based) instead?

This was a constant internal struggle that I had when developing v4.
In fact, several times I had thought about scrapping v4 and re-writing it to be network-proxy based instead.

It was quite the effort digging through the client's memory and figuring out static and dynamic memory offsets
for variables. Many, many times I'd get frustrated and ask myself why am I taking the half-measure of being memory-based instead!

The primary reason was that I was concerned that SleepHunter would become a defacto bot, even if not my intentions.
It was already quite powerful, and I was toeing a line of ethics in being somewhat measured in what I released to the public.

For those who know me and my history in-game, I am no stranger to the more "questionable" methods of play.
Had my fair share of malarkey, shennanigans, and downright tom-foolery. Good memories indeed.

However, I had seen what could happen if a program of that magnitude was released for everyone.
I remember the days of Eru's zero-line client going around. I remember my infamous Injector that also got around.
I remembered other developers' bots and "hacks" that got passed around quite freely.

Ultimately, I did not want to see that happen again and ruin whatever was left of the game at the time.

Another reason was that if SleepHunter acted like a network proxy between the client and server, it becomes a potential point of failure.
If it crashes, you disconnect. If there's a bug in the network code, you might crash or disconnect. With memory-based, it's an entirely "passive" approach the client is unaffected.

The other limitation is that you can't attach post-login. So if you decided you wanted to macro suddenly, you would have to log out and re-launch through the SleepHunter proxy application instead.
This felt like a hassle, whereas with reading process memory you can do that anytime.

There are certainly pros and cons to each approach. I eventually sided with controlled power, reliability, and ease of use.

## What is the license?

SleepHunter is licensed under the [MIT License](./LICENSE.md).
