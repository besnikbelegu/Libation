Libation v12.8.0 - macOS Release Notes
========================================

If the app icon doesn't appear after installation:

1. Move Libation.app to /Applications/
2. Run these commands in Terminal to clear the icon cache:

   sudo rm -rf /Library/Caches/com.apple.iconservices.store
   killall Dock
   killall Finder

3. The Dock and Finder will restart, and the icon should appear.

Alternatively, you can simply restart your Mac.
