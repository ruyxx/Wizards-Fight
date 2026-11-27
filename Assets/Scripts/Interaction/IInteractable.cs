// Zajednički ugovor za predmete sa kojima igrač može da stupi u interakciju.
namespace AWizardsFight
{
    public interface IInteractable
    {
        string Prompt { get; }

        bool CanInteract(PlayerInteractor player);
        bool Interact(PlayerInteractor player);
    }
}
