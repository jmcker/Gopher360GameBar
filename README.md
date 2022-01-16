# Gopher360GameBar #

Add an Xbox Game Bar widget for toggling [Gopher360](https://github.com/Tylemagne/Gopher360).

Gopher360 allows you to control your mouse and keyboard with an Xbox remote.

When controlling your PC from afar, switching between Gopher360 (for quick keyboard/mouse tasks)
and normal Xbox input (games, Xbox Game Bar, Steam, etc) would normally require launching Gopher360
with a keyboard and mouse.

This application makes this possible with just an Xbox remote.


## Notes ##

- Switching application windows and desktops with just the mouse is a lot easier with "Show Task View button" enabled in the task bar.

- Gopher360 does not disable normal Xbox input.
Applications that recognize Xbox navigation, will respond to both Xbox navigation and the Gopher360 keyboard/mouse navigation.
This isn't a problem for most applications, but be careful where Xbox focus is when clicking with Gopher360.
Both the Xbox focused element and the mouse focused element may be triggered.

- The Xbox Game Bar does not focus into the Gopher360GameBar widget. This prevents access to the start/stop button from the game bar.
No access to the start button was mitigated by starting Gopher360 by default when the widget is launched.
Stop button access is only available via the full UWP app outside the game bar.
Adding a shortcut for the UWP app to start menu or taskbar is useful so that you can get to the stop button with the Gopher360 controlled mouse.
Nothing listed in https://docs.microsoft.com/en-us/windows/apps/design/input/gamepad-and-remote-interactions seemed to help the focus issues.
