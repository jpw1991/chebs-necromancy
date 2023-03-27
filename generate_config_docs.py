import re
import os
import fnmatch
import json


def main():
    parsed = parse_config_file(
        '/home/joshua/.local/share/Steam/steamapps/common/Valheim/BepInEx/config/com.chebgonaz.ChebsNecromancy.cfg')
    print(render_markdown(parsed))


def parse_config_file(filepath):
    result = {}
    with open(filepath, 'r') as file:
        contents = file.readlines()
        contents = [x.strip() for x in contents]
        current_key = None
        for line in contents:
            if len(line) < 1:
                continue
            #print(f'line={line}')
            if line[0] == '[' and line[-1] == ']':
                current_key = line[1:-1]
            if current_key is not None and current_key not in result:
                result[current_key] = []
                continue
            if current_key is not None:
                bah = result[current_key]
                bah.append(line)
                result[current_key] = bah
    
    sigh = {}
    for key in result:
        sigh[key] = []
        lines = result[key]
        subdict = {}
        comments = []
        for line in lines:
            if line[0] == "#":
                comments.append(line)
            else:
                subdict[line] = comments
                comments = []
        sigh[key].append(subdict)
    
    return sigh


def render_markdown(data):
    markdown = "# Configs\n\n"

    for key in data.keys():
        markdown += f"## {key}\n\n"

        table_data = data[key][0]

        markdown += "| Setting | Description | Type | Default Value |\n"
        markdown += "| ------- | ----------- | ---- | ------------- |\n"

        try:
            for setting, info in table_data.items():
                markdown += f"| `{setting.split(' ')[0]}` | {info[0][3:]} | `{info[1][2:]}` | `{info[2][2:]}` |\n"
        except:
            pass

        markdown += "\n"

    return markdown


if __name__ == '__main__':
    main()
