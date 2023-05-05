from .hotkey_checker import hotkey_checker

def register_hotkey(key, modifiers, press_callback, release_callback=None):
	return hotkey_checker.register_hotkey(key, modifiers, press_callback, release_callback)

def remove_hotkey(key, modifiers):
	return hotkey_checker.remove_hotkey(key, modifiers)

def start_checking_hotkeys():
	hotkey_checker.start_checking_hotkeys()

def stop_checking_hotkeys():
	hotkey_checker.shutdown_checker()

def restart_checking_hotkeys():
	hotkey_checker.restart_checker()

def register_hotkeys(bindings):
    for binding, keydown_function, keyup_function in bindings:
        key = binding[-1]
        modifiers = []
        for i in range(0, len(binding) - 1):
            modifiers.append(binding[i])

        register_hotkey(key, modifiers, keydown_function, keyup_function)

def remove_hotkeys(bindings):
    for binding in bindings:
        # To accommodate simply reusing the full binding definition ([key_combo], key-down_handler, key-up handler), we'll get just the keybinding.
        if isinstance(binding[0], list):
            binding = binding[0]
        
        key = binding[-1]
        modifiers = []
        for i in range(0, len(binding) - 1):
            modifiers.append(binding[i])

        remove_hotkey(key, modifiers)

# Remove all keybindings.
# *Note that this also stops the hotkey checking thread.
def clear_hotkeys():
    hotkey_checker.clear_bindings()