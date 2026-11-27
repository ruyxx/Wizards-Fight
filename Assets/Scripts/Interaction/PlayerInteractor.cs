// Bira dostupnu interakciju u blizini igrača i izvršava je pritiskom na E.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWizardsFight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(.1f)]
        private float interactionRange = 1.5f;
        [SerializeField]
        private LayerMask interactionLayers = 1 << 9;
        [SerializeField]
        private LayerMask obstructionLayers = 1 << 9;
        private readonly List<Collider2D> nearby = new List<Collider2D>(16);
        private ContactFilter2D filter;
        private PlayerHealth health;
        public IInteractable Current { get; private set; }
        public string CurrentPrompt { get; private set; } = "";

        public event Action<string> PromptChanged;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            filter = new ContactFilter2D
            {
                useTriggers = true
            };
            filter.SetLayerMask(interactionLayers);
        }

        private void Update()
        {
            RefreshTarget();
            if (Application.isFocused && (Keybinds.Pressed(GameAction.Interact) || GamepadInput.InteractPressed))
                TryInteract();
        }

        private void OnDisable() => SetTarget(null);

        public bool CanReach(IInteractable target, float range = float.PositiveInfinity)
        {
            var component = target as MonoBehaviour;
            if (!isActiveAndEnabled
                || health == null
                || health.IsDead
                || Time.timeScale <= 0
                || component == null
                || !component.isActiveAndEnabled)
                return false;
            if (Vector2.Distance(transform.position, component.transform.position) > Mathf.Min(range, interactionRange))
                return false;
            var hit = Physics2D.Linecast(transform.position, component.transform.position, obstructionLayers);
            return !hit || hit.collider.GetComponentInParent<IInteractable>() == target;
        }

        public void RefreshTarget()
        {
            if (!isActiveAndEnabled || health == null || health.IsDead || Time.timeScale <= 0)
            {
                SetTarget(null);
                return;
            }

            int count = Physics2D.OverlapCircle(transform.position, interactionRange, filter, nearby);
            IInteractable best = null;
            float bestDistance = float.PositiveInfinity;
            int bestId = int.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = nearby[i].GetComponentInParent<IInteractable>();
                var component = candidate as MonoBehaviour;
                if (component == null || !candidate.CanInteract(this) || !CanReach(candidate))
                    continue;
                float distance = ((Vector2)(component.transform.position - transform.position)).sqrMagnitude;
                int id = component.GetInstanceID();
                if (distance < bestDistance || (Mathf.Approximately(distance, bestDistance) && id < bestId))
                {
                    best = candidate;
                    bestDistance = distance;
                    bestId = id;
                }
            }

            SetTarget(best);
        }

        public bool TryInteract()
        {
            RefreshTarget();
            if (Current == null)
                return false;
            bool success = Current.Interact(this);
            RefreshTarget();
            return success;
        }

        private void SetTarget(IInteractable target)
        {
            Current = target;
            string prompt = target != null ? target.Prompt : "";
            if (prompt == CurrentPrompt)
                return;
            CurrentPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }
    }
}
