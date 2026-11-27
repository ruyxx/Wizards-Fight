// Podrška za džojstik (Xbox raspored): levi štap kretanje, desni štap nišanjenje i pucanje,
// A izmicanje, B napitak, X interakcija, Y inventar, LB/RB sledeće oružje i posebna čarolija, Back mapa, Start pauza.
using UnityEngine;

namespace AWizardsFight
{
    public static class GamepadInput
    {
        private const float Dead = .25f;
        private static float lastCheck = -10f;
        private static bool connected;

        // Provera povezanih džojstika najviše jednom u sekundi.
        public static bool Connected
        {
            get
            {
                if (Time.unscaledTime - lastCheck > 1f)
                {
                    lastCheck = Time.unscaledTime;
                    connected = false;
                    foreach (string name in Input.GetJoystickNames())
                        connected |= !string.IsNullOrEmpty(name);
                }
                return connected;
            }
        }

        private static float Axis(string name)
        {
            try
            {
                return Input.GetAxisRaw(name);
            }
            catch (System.ArgumentException)
            {
                return 0f;
            }
        }

        public static Vector2 Move
        {
            get
            {
                // Virtuelni džojstik na telefonu ima prednost.
                if (TouchInput.Move.sqrMagnitude > .0001f)
                    return Vector2.ClampMagnitude(TouchInput.Move, 1f);
                if (!Connected)
                    return Vector2.zero;
                var stick = new Vector2(Axis("JoyMoveX"), Axis("JoyMoveY"));
                var dpad = new Vector2(Axis("JoyDpadX"), Axis("JoyDpadY"));
                var value = stick.magnitude > Dead ? stick : dpad;
                return value.magnitude > Dead ? Vector2.ClampMagnitude(value, 1f) : Vector2.zero;
            }
        }

        public static Vector2 Aim
        {
            get
            {
                if (!Connected)
                    return Vector2.zero;
                var stick = new Vector2(Axis("JoyAimX"), Axis("JoyAimY"));
                return stick.magnitude > Dead ? stick : Vector2.zero;
            }
        }

        public static bool Fire => Connected && (Aim.sqrMagnitude > .2f || Axis("JoyRT") > .5f);
        // Dugmad džojstika ili odgovarajuća dugmad na ekranu telefona.
        public static bool DashPressed => Input.GetKeyDown(KeyCode.JoystickButton0) || TouchInput.Pressed(TouchInput.Button.Dash);
        public static bool PotionPressed => Input.GetKeyDown(KeyCode.JoystickButton1) || TouchInput.Pressed(TouchInput.Button.Potion);
        public static bool InteractPressed => Input.GetKeyDown(KeyCode.JoystickButton2) || TouchInput.Pressed(TouchInput.Button.Interact);
        public static bool InventoryPressed => Input.GetKeyDown(KeyCode.JoystickButton3) || TouchInput.Pressed(TouchInput.Button.Inventory);
        public static bool NextWeaponPressed => Input.GetKeyDown(KeyCode.JoystickButton4) || TouchInput.Pressed(TouchInput.Button.NextWeapon);
        public static bool SpecialPressed => Input.GetKeyDown(KeyCode.JoystickButton5) || TouchInput.Pressed(TouchInput.Button.Special);
        public static bool MapPressed => Input.GetKeyDown(KeyCode.JoystickButton6) || TouchInput.Pressed(TouchInput.Button.Map);
        public static bool PausePressed => Input.GetKeyDown(KeyCode.JoystickButton7) || TouchInput.Pressed(TouchInput.Button.Pause);
    }
}
