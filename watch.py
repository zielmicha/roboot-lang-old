#!/usr/bin/python3
import subprocess, sys
files = subprocess.check_output("find | grep -vP '^./(obj|bin|\.git|\.mypy_cache)'", shell=True).decode().splitlines()
while True:
    p = subprocess.Popen(['inotifywait', '-e', 'close_write', *files])
    subprocess.call(['clear'])
    subprocess.call(sys.argv[1:])
    p.wait()
