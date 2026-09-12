import re

with open('Assets/Plugins/Android/mainTemplate.gradle', 'r') as f:
    content = f.read()

if '// Android Resolver Dependencies Start' not in content:
    content = content.replace('**DEPS**}', '''**DEPS**

// Android Resolver Dependencies Start
// Android Resolver Dependencies End
}''')
    with open('Assets/Plugins/Android/mainTemplate.gradle', 'w') as f:
        f.write(content)
