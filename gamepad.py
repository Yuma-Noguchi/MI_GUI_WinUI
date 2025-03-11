from abc import ABC, abstractmethod
from time import sleep
from typing import Optional, TypeVar, Generic

from lib.drivers.base.driver import Driver

GamepadType = TypeVar('GamepadType')
ButtonType = TypeVar('ButtonType')


class GamepadBase(Driver, Generic[GamepadType, ButtonType], ABC):
    """
    Abstract base class for virtual gamepad drivers.

    Handles batch mode for update calls and defines the interface for common gamepad actions.
    """

    def __init__(self) -> None:
        super().__init__()

        # When the driver is initialized, the gamepad object is not created.
        # It is created when the gamepad property is accessed.
        # This lazy loading ensures that a virtual controller is not connected until it is needed.
        self._gamepad: Optional[GamepadType] = None
        self._batch_mode = False
        self._held_buttons = set()

    @abstractmethod
    def _initialize_gamepad(self) -> None:
        """Initialize the gamepad object."""
        pass

    @property
    def gamepad(self) -> GamepadType:
        """Return the gamepad instance."""
        if self._gamepad is None:
            self._initialize_gamepad()

        return self._gamepad

    @abstractmethod
    def get_button(self, button: str | int) -> ButtonType:
        """
        Convert a button string or integer to the corresponding button type.

        Args:
            button (str|int): The button to convert.

        Returns:
            DS4_BUTTONS|XUSB_BUTTON: The converted button type.
        """
        ...

    def button_down(self, *buttons: str) -> None:
        """
        Press a button(s) on the gamepad.

        Args:
            buttons (str): The buttons to press.
        """
        # Convert to button types first so errors are caught early and not halfway through batch
        buttons = [self.get_button(button) for button in buttons]
        for button in buttons:
            self.gamepad.press_button(button=button)

        self.update()

    buttons_down = button_down

    def button_up(self, *buttons: str) -> None:
        """
        Release a button(s) on the gamepad.

        Args:
            buttons (str): The buttons to release.
        """
        buttons = [self.get_button(button) for button in buttons]
        for button in buttons:
            self.gamepad.release_button(button=button)

        self.update()

    release = button_up
    release_buttons = button_up
    buttons_up = button_up

    def hold_button(self, *buttons: str, duration: float = 0.0) -> None:
        """
        Press and hold a button(s) on the gamepad.

        Args:
            buttons (str): The buttons to hold.
            duration (float): The duration to hold the button.
        """
        if isinstance(buttons[-1], (int, float)):
            duration = buttons[-1]
            buttons = buttons[:-1]

        self.button_down(*buttons)

        if duration > 0:
            sleep(duration)
            self.button_up(*buttons)

    hold = hold_button
    hold_buttons = hold_button

    def press_button(self, *buttons: str, times: int = 1) -> None:
        """
        Press a button on the gamepad.

        Args:
            buttons (str|int): The buttons to press.
            times (int): The number of times to press the button.
        """
        if isinstance(buttons[-1], (int, float)):
            times = buttons[-1]
            buttons = buttons[:-1]

        for _ in range(times):
            self.hold_button(*buttons, duration=0.1)

    press = press_button
    click = press_button
    press_buttons = press_button

    def combo(self, button: str, times: int, interval: float = 0.1) -> None:
        """
        Press a button on the gamepad multiple times with an interval.

        Args:
            button (str): The button to press.
            times (int): The number of times to press the button.
            interval (float): The interval between each press.
        """
        for _ in range(times):
            self.hold_button(button, duration=0.1)
            sleep(interval)

    combo_click = combo
    combo_press = combo

    def toggle_button(self, *buttons: str) -> None:
        """
        Toggle a button(s) on the gamepad.

        Args:
            buttons (str): The buttons to toggle.
        """
        buttons = [self.get_button(button) for button in buttons]
        for button in buttons:
            if button in self._held_buttons:
                self.gamepad.release_button(button=button)
                self._held_buttons.remove(button)
            else:
                self.gamepad.press_button(button=button)
                self._held_buttons.add(button)

        self.update()

    toggle = toggle_button
    toggle_buttons = toggle_button

    def left_joystick(self, x: float = 0.0, y: float = 0.0) -> None:
        """
        Set the position of the left joystick.

        Args:
            x (float): X-axis value between -1.0 and 1.0.
            y (float): Y-axis value between -1.0 and 1.0.
        """
        self.gamepad.left_joystick_float(x_value_float=x, y_value_float=y)
        self.update()

    def right_joystick(self, x: float = 0.0, y: float = 0.0) -> None:
        """
        Set the position of the right joystick.

        Args:
            x (float): X-axis value between -1.0 and 1.0.
            y (float): Y-axis value between -1.0 and 1.0.
        """
        self.gamepad.right_joystick_float(x_value_float=x, y_value_float=y)
        self.update()

    def left_trigger(self, value: float = 0.0) -> None:
        """
        Set the position of the left trigger.

        Args:
            value (float): Trigger value between 0.0 and 1.0.
        """
        self.gamepad.left_trigger_float(value_float=value)
        self.update()

    def right_trigger(self, value: float = 0.0) -> None:
        """
        Set the position of the right trigger.

        Args:
            value (float): Trigger value between 0.0 and 1.0.
        """
        self.gamepad.right_trigger_float(value_float=value)
        self.update()

    def update(self) -> None:
        """
        Update the gamepad state.

        Flush any pending inputs to the virtual gamepad.
        """
        if not self._batch_mode:
            self.gamepad.update()

    def reset(self) -> None:
        """Reset the gamepad state."""
        self.gamepad.reset()
        self.update()

    def __enter__(self) -> "GamepadBase[GamepadType, ButtonType]":
        """
        Enter batch mode to chain multiple inputs without immediate update.

        Returns:
            GamepadBase: The gamepad instance itself.
        """
        self._batch_mode = True
        return self

    def __exit__(self, exc_type, exc_value, traceback) -> None:
        """Exit batch mode and update the gamepad state."""
        self._batch_mode = False
        self.gamepad.update()
