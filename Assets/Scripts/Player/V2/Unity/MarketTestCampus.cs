using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CrazyMarket.Player.V2.Unity
{
    // Scene-local reset and navigation only; locomotion and abilities belong to V2.
    public sealed class MarketTestCampus : MonoBehaviour
    {
        [SerializeField] private PlayerControllerV2 player;
        [SerializeField] private Transform[] stations;
        [SerializeField] private Rigidbody[] physicsProps;
        [SerializeField] private UIDocument document;
        private Vector3[] propPositions;
        private Quaternion[] propRotations;
        private int currentStation;
        private ButterBody butterBody;
        private Label butterStatus;
        private const string Controls = "MARKET LAB  /  V2\n1 AISLES    2 MOVEMENT    3 PHYSICS    4 ABILITIES\nF2 / SELECT  RESET    •    WASD / STICK  MOVE    •    SPACE / A  JUMP";

        private void Start()
        {
            propPositions = new Vector3[physicsProps.Length];
            propRotations = new Quaternion[physicsProps.Length];
            for (int i = 0; i < physicsProps.Length; i++)
            {
                propPositions[i] = physicsProps[i].position;
                propRotations[i] = physicsProps[i].rotation;
            }
            player.SetMovementEnabled(true);
            var root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            var strip = new Label(Controls);
            strip.pickingMode = PickingMode.Ignore;
            strip.style.position = Position.Absolute;
            strip.style.left = 24;
            strip.style.bottom = 24;
            strip.style.paddingLeft = strip.style.paddingRight = 20;
            strip.style.paddingTop = strip.style.paddingBottom = 12;
            strip.style.backgroundColor = new Color(1f, .976f, .90f, .96f);
            strip.style.color = new Color(.125f, .145f, .26f);
            strip.style.fontSize = 19;
            strip.style.unityFontStyleAndWeight = FontStyle.Bold;
            strip.style.borderLeftWidth = 8;
            strip.style.borderLeftColor = new Color(.25f, .8f, .64f);
            strip.style.borderTopLeftRadius = strip.style.borderTopRightRadius = 16;
            strip.style.borderBottomLeftRadius = strip.style.borderBottomRightRadius = 16;
            root.Add(strip);
            butterBody = player.GetComponent<ButterBody>();
            if (butterBody != null)
            {
                butterStatus = strip;
            }
        }

        private void Update()
        {
            if (butterStatus != null)
                butterStatus.text = Controls + $"\nBUTTER {butterBody.Reserve:P0}  /  {butterBody.RemainingDashes} DASHES    •    SHIFT / RT  DASH    •    R / LB  RECALL";
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) GoToStation(0);
                if (keyboard.digit2Key.wasPressedThisFrame) GoToStation(1);
                if (keyboard.digit3Key.wasPressedThisFrame) GoToStation(2);
                if (keyboard.digit4Key.wasPressedThisFrame) GoToStation(3);
            }
            if (keyboard != null && keyboard.f2Key.wasPressedThisFrame ||
                Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
                ResetCampus();
            if (player.transform.position.y < -10f)
                GoToStation(currentStation);
            for (int i = 0; i < physicsProps.Length; i++)
                if (physicsProps[i] != null && physicsProps[i].position.y < -10f)
                    ResetProp(i);
        }

        public void GoToStation(int index)
        {
            if (index < 0 || index >= stations.Length || stations[index] == null) return;
            currentStation = index;
            player.TeleportTo(stations[index].position, stations[index].rotation);
        }

        public void ResetCampus()
        {
            for (int i = 0; i < physicsProps.Length; i++)
                if (physicsProps[i] != null) ResetProp(i);
            GoToStation(currentStation);
        }

        private void ResetProp(int index)
        {
            var body = physicsProps[index];
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = propPositions[index];
            body.rotation = propRotations[index];
            body.Sleep();
        }
    }
}
