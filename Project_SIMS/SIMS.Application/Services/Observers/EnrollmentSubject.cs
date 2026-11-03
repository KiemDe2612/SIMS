using SIMS.Application.Interfaces;

namespace SIMS.Application.Services.Observers;

public class EnrollmentSubject : IEnrollmentSubject
{
    private readonly List<IEnrollmentObserver> _observers = new();

    public EnrollmentSubject(IEnumerable<IEnrollmentObserver>? initialObservers = null)
    {
        if (initialObservers != null)
        {
            _observers.AddRange(initialObservers);
        }
    }

    public IReadOnlyList<IEnrollmentObserver> Observers => _observers.AsReadOnly();

    public void Attach(IEnrollmentObserver observer)
    {
        if (observer != null && !_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void Detach(IEnrollmentObserver observer)
    {
        if (observer != null)
        {
            _observers.Remove(observer);
        }
    }

    public async Task NotifyAsync(EnrollmentCreatedEvent eventData)
    {
        foreach (var observer in _observers.ToList())
        {
            try
            {
                await observer.OnEnrollmentCreatedAsync(eventData);
            }
            catch
            {
                // Prevent one observer failure from breaking other observers
            }
        }
    }
}

