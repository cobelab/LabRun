# -*- coding: utf-8 -*-
"""
Run a python script and save error + traceback to a log file in case of a crash.

By Jonas Lindeløv, jonas@cnru.dk
No copyright, no attribution required. Use as you please :-)
"""
import sys
import os


try:
    dir = os.path.dirname(sys.argv[1])
    os.chdir(dir)
    # Run the file
    execfile(sys.argv[1])
except Exception:
    # If there's an error, save it (with traceback) to a log file
    with open('error.log', 'a') as errorFile:  # error file. Appends if it exists.
        import traceback
        errorFile.write(str(traceback.format_exc()))  # write traceback, includes error
        
    print traceback.format_exc()  # still print it, in case people keep the command window.