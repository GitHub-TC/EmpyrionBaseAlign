# Empyrion Admin Tools

## FAQ

### What is this?

In talking with a couple of server owners, there were a couple of clear request for functionality that the game either doesn't provide or makes hard to access.  Admins want the ability to easily teleport players around without using EAH, prefereably from inside the game, and they want a mailbox that they can use to send gifts to players.  

As a note, when I went to go build out that functionality, I realized what a pain it would be with the existing API, so I built a framework to make it easier (see: https://github.com/lostinplace/EmpyrionAPITools) It's in use for this mod.

### How do I use it?

The easiest way to use this is to copy the contents of the `build` folder into your server's `Content/Mods` folder.  Restart your server and it's loaded.

When you log into the game, you can bring up the chat text input using either the `.` or `/` keys.  You can then type `/at help` for "AdminTools Help".  That will bring up a dialog box that lists the commands that you have access to.

#### What are all the commands?

#### Help

`/at help` : discussed above

#### Teleport

`/at teleport {targetPlayer} to {destinationPlayer}`

This allows you to move players around by name.  This command is only available to moderators and above, because you should really have `gm` mode on while using it.  If you don't want to type out your own name, you can enter "me" as a shorthand.  It will try to look up active player names in the following order:

1. Case insensitve containment search
2. Case sensitive containment search
3. Exact match

Note that the teleport command will only execute if the provided names both resolve to one and only one player.

#### Mailbox

`/at mailbox`

`/at mailbox {playerName}`

By using this invocation, you can bring up an inventory window for temporarily storing items outside of your character's body, or receivng gifts from admins.  This means that your mailbox can be used to store your items when you log off, so you don't have to worry about waking up in space and losing everything.

As an admin, you can bring up the mailbox for a player, and load items for them to access regardless of where they are.

### Is that it?

Yup, that's it for now.  If you have anything you'd like to see added, let me know, or send me a pull request.

---

Have fun!
