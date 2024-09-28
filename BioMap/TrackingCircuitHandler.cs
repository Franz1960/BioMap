using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System;

namespace BioMap
{
  public class TrackingCircuitHandler : CircuitHandler
  {
    private readonly HashSet<Circuit> circuits = new();

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken) {
      this.circuits.Add(circuit);
      Utilities.FireEvent(this.ConnectedCircuitsCountChanged, this, EventArgs.Empty);
      return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken) {
      this.circuits.Remove(circuit);
      Utilities.FireEvent(this.ConnectedCircuitsCountChanged, this, EventArgs.Empty);
      return Task.CompletedTask;
    }

    public event EventHandler ConnectedCircuitsCountChanged;

    public int ConnectedCircuitsCount => this.circuits.Count;
  }
}
