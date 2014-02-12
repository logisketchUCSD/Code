#-------------------------------------------------------------------------#
################################## FILES ##################################
#-------------------------------------------------------------------------#

This project contains some general file management subroutes. The eventual
goal is centralized management of file extensions, and perhaps even of file
loading and such (although no such progress has yet been made).

Files is a class for simultaneously loading many files. It supports path
inclusion and exclusion, recursive searches, and many other useful features.

FUtil is a static class for dealing with filenames and extensions. It
provides enums for filetypes (instead of passing around strings), and can
convert freely between extensions and filetypes. It also provides utility
functions for dealing with Windows.Forms save and load dialogs.
