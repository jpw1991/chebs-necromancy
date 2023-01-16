import re
import os
import json

localization_strings = {}
for root, dirs, files in os.walk("ChebsNecromancy"):
    path = root.split(os.sep)
    for file in files:
        if file.endswith(".cs"):
            with open(os.sep.join(path+[file]), 'r') as f:
                for line in f.readlines():
                    x = re.findall('''"\$[A-Za-z_]+"''', line)
                    if x and len(x) > 0:
                        localization_strings[x[0].replace('"', '')] = ''
print(json.dumps(localization_strings, indent=4))
