// Zajednički ugovor za objekte koji mogu da prime štetu.
namespace AWizardsFight
{
    
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    // Objekat koji može da ublaži pogodak projektila, npr. štit štitonoše (1 = puna šteta).
    public interface IProjectileBlocker
    {
        float DamageFactor(UnityEngine.Vector2 projectileDirection, UnityEngine.Vector2 impactPoint);
    }
}
