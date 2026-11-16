using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Stack of scopes. Each maps a name to one or more references; more than one means the name is ambiguous.
/// Lookup goes from the innermost scope outwards and the first scope that has the name wins.
/// </summary>
internal sealed class BindingContext(DiagnosticBag diagnostics)
{
    private readonly List<Dictionary<string, List<Ref>>> _frames = [];

    /// <summary>The imports of each file, resolved the first time the file is visited.</summary>
    public Dictionary<TqFileScope, (List<(string Name, Ref Reference)> Explicit, List<(string Name, Ref Reference)> Star)>
        ImportCache { get; } = [];

    /// <summary>The namespace of the code being bound; what it can reach depends on it.</summary>
    public AccessContext Accessor { get; set; }

    public void PushFrame() => _frames.Add(new Dictionary<string, List<Ref>>());

    public void PopFrame()
    {
        if (_frames.Count == 0) throw new InvalidOperationException("No frame to pop");
        _frames.RemoveAt(_frames.Count - 1);
    }

    public void Add(IMember member) => Add(member.Name, member.GetReference());

    public void Add(string name, Ref reference)
    {
        var frame = _frames[^1];
        if (!frame.TryGetValue(name, out var candidates))
        {
            candidates = new List<Ref>();
            frame.Add(name, candidates);
        }

        if (!candidates.Contains(reference)) candidates.Add(reference);
    }

    public bool IsDeclaredInCurrentFrame(string name) => _frames[^1].ContainsKey(name);

    /// <summary>
    /// Null if the name does not exist or is ambiguous; `candidates` holds what the winning scope has for it.
    /// Members the code cannot reach are left out, unless that leaves nothing: then they are kept, so the
    /// caller can say they are inaccessible instead of saying they do not exist.
    /// </summary>
    public Ref? TryGet(string name, out IReadOnlyList<Ref> candidates)
    {
        for (var i = _frames.Count - 1; i >= 0; i--)
        {
            if (!_frames[i].TryGetValue(name, out var found)) continue;

            var accessible = found.Where(CanAccess).ToList();
            candidates = accessible.Count > 0 ? accessible : found;
            return candidates.Count == 1 ? candidates[0] : null;
        }

        candidates = Array.Empty<Ref>();
        return null;
    }

    public bool CanAccess(Ref reference) => AccessChecker.CanAccess(AccessChecker.MemberOf(reference), Accessor);

    /// <summary>Reports the use of a member the current code cannot reach.</summary>
    public void CheckAccess(string name, Ref reference, SourceLocation location)
    {
        var member = AccessChecker.MemberOf(reference);
        if (member != null && !AccessChecker.CanAccess(member, Accessor))
            Error(AccessChecker.Explain(name, member, Accessor), location);
    }

    public void Error(string message, SourceLocation location) =>
        diagnostics.ReportError(new BindingError(message, location));
}
