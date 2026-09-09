namespace Heltevagten;

/// <summary>
/// Represents the shared base for all heroes in the dispatch system.
/// </summary>
public abstract class Hero
{
    private const int MinimumEnergyLevel = 0;
    private const int MaximumEnergyLevel = 100;

    private int _energyLevel;
    
    /// <summary>
    /// Gets the hero's name.
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Gets the hero's current energy level.
    /// </summary>
    public int EnergyLevel => _energyLevel;
    
    /// <summary>
    /// Gets whether the hero is currently available for an incident.
    /// </summary>
    public bool IsAvailable { get; set; }
    
    
    /// <summary>
    /// Initializes a new hero with a name and an energy level.
    /// </summary>
    /// <param name="name">The hero's name.</param>
    /// <param name="energyLevel">The initial energy level from 0 to 100.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the name is empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the energy level is outside the allowed range.
    /// </exception>
    protected  Hero(string name, int energyLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentNullException("A hero must have a name", nameof(name));
        }

        if (energyLevel is < MinimumEnergyLevel or > MaximumEnergyLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(energyLevel), $"{energyLevel} must be between {MinimumEnergyLevel} and {MaximumEnergyLevel}");
        }
        
        Name = name;
        _energyLevel = MinimumEnergyLevel;
        IsAvailable = true;
    }

    /// <summary>
    /// Uses some of the hero's available energy.
    /// </summary>
    /// <param name="amount">The amount of energy to use.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the amount is zero or negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the hero does not have enough energy.
    /// </exception>
    public void UseEnergy(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"{amount} must be greater than 0");
        }

        if (amount > _energyLevel)
        {
            throw new InvalidOperationException($"{Name} doesn't have enough energy");
        }
        _energyLevel -= amount;
    }
    
    /// <summary>
    /// Restores energy without allowing the level to exceed 100.
    /// </summary>
    /// <param name="amount">The amount of energy to restore.</param>
    public void RestoreEnergy(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"{amount} must be greater than 0");
        }
        
        _energyLevel = Math.Min(_energyLevel + amount, MaximumEnergyLevel);
    }
    
    /// <summary>
    /// Changes whether the hero is available for assignment.
    /// Intended for use by the domain and dispatch logic.
    /// </summary>
    internal void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
    }
    
    /// <summary>
    /// Performs the hero's unique signature move.
    /// </summary>
    /// <returns>A description of the performed move.</returns>
    public abstract string UseSignatureMove();
}