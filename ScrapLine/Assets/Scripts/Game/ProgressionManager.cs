using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ObjectiveStateSnapshot
{
    public string ObjectiveId { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsRewardClaimed { get; set; }
}

/// <summary>
/// Single authority for deterministic objective progress and reward claims.
/// Objective state is advisory and is never consulted by gameplay capability APIs.
/// </summary>
public sealed class ProgressionManager : MonoBehaviour
{
    public const string ObjectiveRewardUnlockSourcePrefix = "objective_reward:";

    private readonly List<ObjectiveDefinition> definitions = new List<ObjectiveDefinition>();
    private readonly Dictionary<string, ObjectiveDefinition> definitionsById =
        new Dictionary<string, ObjectiveDefinition>(StringComparer.Ordinal);
    private GameData gameData;
    private CreditsManager creditsManager;
    private bool initialized;

    public event Action<ObjectiveStateSnapshot> ObjectiveProgressed;
    public event Action<ObjectiveStateSnapshot> ObjectiveCompleted;
    public event Action<ObjectiveStateSnapshot> ObjectiveRewardClaimed;
    public event Action ObjectiveStateReloaded;

    public IReadOnlyList<ObjectiveDefinition> Definitions => definitions.AsReadOnly();

    public IReadOnlyList<ObjectiveStateSnapshot> ObjectiveStates => definitions
        .Select(definition => CreateSnapshot(definition, FindState(definition.id)))
        .ToList();

    public bool Initialize(CreditsManager credits, string definitionsJson = null)
    {
        creditsManager = credits;
        if (string.IsNullOrWhiteSpace(definitionsJson))
        {
            TextAsset asset = Resources.Load<TextAsset>("objectives");
            if (asset == null)
            {
                GameLogger.LogError(LoggingManager.LogCategory.Debug,
                    "Missing Resources/objectives.json; progression is disabled.");
                return false;
            }
            definitionsJson = asset.text;
        }

        if (!TryLoadDefinitions(definitionsJson, out string error))
        {
            GameLogger.LogError(LoggingManager.LogCategory.Debug,
                $"Objective definitions are invalid: {error}");
            return false;
        }

        GameplayDomainEvents.EventPublished -= HandleDomainEvent;
        GameplayDomainEvents.EventPublished += HandleDomainEvent;
        initialized = true;
        return true;
    }

    private void OnDestroy()
    {
        GameplayDomainEvents.EventPublished -= HandleDomainEvent;
    }

    public bool TryLoadDefinitions(string json, out string error)
    {
        error = null;
        ObjectiveDefinitionList parsed;
        try
        {
            parsed = JsonUtility.FromJson<ObjectiveDefinitionList>(json);
        }
        catch (Exception exception)
        {
            error = $"objectives.json could not be parsed: {exception.Message}";
            return false;
        }

        if (parsed?.objectives == null || parsed.objectives.Count == 0)
        {
            error = "objectives.json must contain at least one objective.";
            return false;
        }

        Dictionary<string, ObjectiveDefinition> candidates =
            new Dictionary<string, ObjectiveDefinition>(StringComparer.Ordinal);
        foreach (ObjectiveDefinition objective in parsed.objectives)
        {
            if (!ValidateDefinition(objective, candidates, out error))
                return false;
            candidates.Add(objective.id, objective);
        }

        foreach (ObjectiveDefinition objective in parsed.objectives)
        {
            if (!string.IsNullOrWhiteSpace(objective.prerequisiteObjectiveId) &&
                !candidates.ContainsKey(objective.prerequisiteObjectiveId))
            {
                error = $"Objective '{objective.id}' references unknown prerequisite " +
                        $"'{objective.prerequisiteObjectiveId}'.";
                return false;
            }
            if (HasPrerequisiteCycle(objective, candidates))
            {
                error = $"Objective '{objective.id}' participates in a prerequisite cycle.";
                return false;
            }
        }

        definitions.Clear();
        definitions.AddRange(parsed.objectives.OrderBy(item => item.presentation?.sortOrder ?? 0)
            .ThenBy(item => item.id, StringComparer.Ordinal));
        definitionsById.Clear();
        foreach (ObjectiveDefinition objective in definitions)
            definitionsById.Add(objective.id, objective);
        return true;
    }

    private static bool ValidateDefinition(
        ObjectiveDefinition objective,
        IReadOnlyDictionary<string, ObjectiveDefinition> existing,
        out string error)
    {
        if (objective == null)
        {
            error = "Objective definition is null.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(objective.id))
        {
            error = "Objective ID is required.";
            return false;
        }
        if (existing.ContainsKey(objective.id))
        {
            error = $"Objective ID '{objective.id}' must be unique.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(objective.title) || string.IsNullOrWhiteSpace(objective.description))
        {
            error = $"Objective '{objective.id}' requires a title and description.";
            return false;
        }
        if (!ObjectiveTypes.Supported.Contains(objective.type))
        {
            error = $"Objective '{objective.id}' has unsupported type '{objective.type}'.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(objective.targetId) || objective.targetValue <= 0)
        {
            error = $"Objective '{objective.id}' requires a target ID and positive targetValue.";
            return false;
        }
        if (!ValidateTargetReference(objective.type, objective.targetId, out error))
        {
            error = $"Objective '{objective.id}' {error}";
            return false;
        }
        if (!ValidateRewardDefinition(objective.id, objective.reward, out error))
            return false;
        return true;
    }

    private static bool ValidateTargetReference(string type, string targetId, out string error)
    {
        error = null;
        FactoryRegistry registry = FactoryRegistry.Instance;
        if (type == ObjectiveTypes.MachinePlaced || type == ObjectiveTypes.MachineLicense)
        {
            if (registry.GetMachine(targetId) == null)
                error = $"references unknown machine '{targetId}'.";
        }
        else if (type == ObjectiveTypes.ItemSoldCount || type == ObjectiveTypes.ItemSoldValue)
        {
            if (registry.GetItem(targetId) == null)
                error = $"references unknown item '{targetId}'.";
        }
        else if (type == ObjectiveTypes.RecipeCompleted)
        {
            if (registry.GetRecipeById(targetId) == null)
                error = $"references unknown recipe '{targetId}'.";
        }
        else if (type == ObjectiveTypes.ScrapDelivery)
        {
            if (registry.GetWasteCrate(targetId) == null)
                error = $"references unknown scrap crate '{targetId}'.";
        }
        else if (type == ObjectiveTypes.GridExpanded &&
                 targetId != "any" && targetId != "row" &&
                 targetId != "column" && targetId != "edge_column")
        {
            error = $"references unsupported expansion type '{targetId}'.";
        }
        return error == null;
    }

    private static bool ValidateRewardDefinition(
        string objectiveId,
        ObjectiveRewardDefinition reward,
        out string error)
    {
        if (reward == null)
        {
            error = $"Objective '{objectiveId}' requires a reward.";
            return false;
        }
        if (reward.type == ObjectiveRewardTypes.Credits)
        {
            if (reward.amount <= 0 || !string.IsNullOrWhiteSpace(reward.targetId))
            {
                error = $"Objective '{objectiveId}' credit reward requires a positive amount and no targetId.";
                return false;
            }
        }
        else if (reward.type == ObjectiveRewardTypes.MachineLicense)
        {
            if (reward.amount != 0 || string.IsNullOrWhiteSpace(reward.targetId) ||
                FactoryRegistry.Instance.GetMachine(reward.targetId) == null)
            {
                error = $"Objective '{objectiveId}' machine-license reward references an invalid machine.";
                return false;
            }
        }
        else
        {
            error = $"Objective '{objectiveId}' has unsupported reward type '{reward.type}'.";
            return false;
        }
        error = null;
        return true;
    }

    private static bool HasPrerequisiteCycle(
        ObjectiveDefinition start,
        IReadOnlyDictionary<string, ObjectiveDefinition> candidates)
    {
        HashSet<string> visited = new HashSet<string>(StringComparer.Ordinal);
        ObjectiveDefinition current = start;
        while (current != null && !string.IsNullOrWhiteSpace(current.prerequisiteObjectiveId))
        {
            if (!visited.Add(current.id))
                return true;
            candidates.TryGetValue(current.prerequisiteObjectiveId, out current);
        }
        return false;
    }

    public bool ResetForNewGame(GameData data, out string error)
    {
        if (!initialized)
        {
            error = "ProgressionManager is not initialized.";
            return false;
        }
        gameData = data;
        gameData.objectiveProgress = definitions.Select(CreateCleanState).ToList();
        error = null;
        Notify(ObjectiveStateReloaded, nameof(ObjectiveStateReloaded));
        return true;
    }

    public bool LoadFromGameData(GameData data, out string error)
    {
        if (!ValidateSavedState(data, out error))
            return false;

        gameData = data;
        gameData.objectiveProgress ??= new List<ObjectiveProgressData>();
        foreach (ObjectiveProgressData state in gameData.objectiveProgress)
            state.processedEventIds ??= new List<string>();
        foreach (ObjectiveDefinition definition in definitions)
        {
            if (FindState(definition.id) == null)
                gameData.objectiveProgress.Add(CreateCleanState(definition));
        }
        Notify(ObjectiveStateReloaded, nameof(ObjectiveStateReloaded));
        return true;
    }

    public bool ValidateSavedState(GameData data, out string error)
    {
        if (data == null)
        {
            error = "Cannot validate objective state in null game data.";
            return false;
        }
        if (data.objectiveProgress == null)
        {
            error = null;
            return true;
        }

        HashSet<string> objectiveIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (ObjectiveProgressData state in data.objectiveProgress)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.objectiveId))
            {
                error = "Saved objective state contains a missing objective ID.";
                return false;
            }
            if (!definitionsById.TryGetValue(state.objectiveId, out ObjectiveDefinition definition))
            {
                error = $"Saved objective state references unknown objective '{state.objectiveId}'.";
                return false;
            }
            if (!objectiveIds.Add(state.objectiveId))
            {
                error = $"Saved objective state contains duplicate objective '{state.objectiveId}'.";
                return false;
            }
            if (state.progress < 0 || state.progress > definition.targetValue)
            {
                error = $"Saved objective '{state.objectiveId}' has impossible progress {state.progress}.";
                return false;
            }
            if (state.completed != (state.progress >= definition.targetValue))
            {
                error = $"Saved objective '{state.objectiveId}' has inconsistent completion state.";
                return false;
            }
            if (state.rewardClaimed && !state.completed)
            {
                error = $"Saved objective '{state.objectiveId}' claims a reward before completion.";
                return false;
            }
            if (state.processedEventIds != null)
            {
                HashSet<string> eventIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (string eventId in state.processedEventIds)
                {
                    if (string.IsNullOrWhiteSpace(eventId) || !eventIds.Add(eventId))
                    {
                        error = $"Saved objective '{state.objectiveId}' contains invalid event identities.";
                        return false;
                    }
                }
            }
        }

        error = null;
        return true;
    }

    public string ValidateSaveCandidate(GameData data)
    {
        return ValidateSavedState(data, out string error) ? null : error;
    }

    public void SaveToGameData(GameData data)
    {
        if (data == null)
            return;
        if (gameData?.objectiveProgress == null)
            data.objectiveProgress ??= new List<ObjectiveProgressData>();
        else
            data.objectiveProgress = gameData.objectiveProgress;
    }

    public bool RecordEvent(
        string eventId,
        string eventType,
        string targetId,
        int count = 1,
        int value = 0,
        string source = null)
    {
        return RecordEvent(new GameplayDomainEvent
        {
            EventId = eventId,
            Type = eventType,
            TargetId = targetId,
            Count = count,
            Value = value,
            Source = source
        });
    }

    public bool RecordEvent(GameplayDomainEvent gameplayEvent)
    {
        if (gameData?.objectiveProgress == null || gameplayEvent == null ||
            string.IsNullOrWhiteSpace(gameplayEvent.EventId))
            return false;

        bool changed = false;
        foreach (ObjectiveDefinition definition in definitions)
        {
            ObjectiveProgressData state = FindState(definition.id);
            if (state == null || state.completed || !Matches(definition, gameplayEvent))
                continue;

            state.processedEventIds ??= new List<string>();
            if (state.processedEventIds.Contains(gameplayEvent.EventId))
                continue;

            int increment = definition.type == ObjectiveTypes.ItemSoldValue
                ? gameplayEvent.Value
                : gameplayEvent.Count;
            if (increment <= 0)
                continue;

            state.processedEventIds.Add(gameplayEvent.EventId);
            state.progress = Math.Min(definition.targetValue, state.progress + increment);
            changed = true;
            ObjectiveStateSnapshot snapshot = CreateSnapshot(definition, state);
            Notify(ObjectiveProgressed, snapshot, nameof(ObjectiveProgressed));
            if (state.progress >= definition.targetValue)
            {
                state.completed = true;
                snapshot = CreateSnapshot(definition, state);
                Notify(ObjectiveCompleted, snapshot, nameof(ObjectiveCompleted));
            }
        }

        if (changed)
            GameManager.Instance?.RequestAutosave();
        return changed;
    }

    private void HandleDomainEvent(GameplayDomainEvent gameplayEvent)
    {
        RecordEvent(gameplayEvent);
    }

    private static bool Matches(ObjectiveDefinition definition, GameplayDomainEvent gameplayEvent)
    {
        bool typeMatches = definition.type == gameplayEvent.Type ||
            (definition.type == ObjectiveTypes.ItemSoldValue &&
             gameplayEvent.Type == ObjectiveTypes.ItemSoldCount);
        if (!typeMatches)
            return false;
        if (definition.type == ObjectiveTypes.GridExpanded && definition.targetId == "any")
            return true;
        return string.Equals(definition.targetId, gameplayEvent.TargetId, StringComparison.Ordinal);
    }

    public bool TryClaimReward(string objectiveId, out string error)
    {
        if (!definitionsById.TryGetValue(objectiveId, out ObjectiveDefinition definition))
        {
            error = $"Unknown objective '{objectiveId ?? "<null>"}'.";
            return false;
        }
        ObjectiveProgressData state = FindState(objectiveId);
        if (state == null || !state.completed)
        {
            error = $"Objective '{objectiveId}' is not complete.";
            return false;
        }
        if (state.rewardClaimed)
        {
            error = $"Objective '{objectiveId}' reward was already claimed.";
            return false;
        }

        ObjectiveRewardDefinition reward = definition.reward;
        if (reward.type == ObjectiveRewardTypes.Credits)
        {
            if (creditsManager == null)
            {
                error = $"Cannot claim '{objectiveId}' without an initialized CreditsManager.";
                return false;
            }
            creditsManager.AddCredits(reward.amount);
        }
        else if (reward.type == ObjectiveRewardTypes.MachineLicense)
        {
            if (!FactoryRegistry.Instance.IsMachineUnlocked(reward.targetId) &&
                !FactoryRegistry.Instance.TryGrantMachineLicense(
                    reward.targetId,
                    ObjectiveRewardUnlockSourcePrefix + objectiveId,
                    out error))
                return false;
        }
        else
        {
            error = $"Objective '{objectiveId}' has an unsupported reward.";
            return false;
        }

        state.rewardClaimed = true;
        error = null;
        Notify(ObjectiveRewardClaimed, CreateSnapshot(definition, state), nameof(ObjectiveRewardClaimed));
        GameManager.Instance?.RequestAutosave();
        return true;
    }

    public ObjectiveDefinition GetDefinition(string objectiveId)
    {
        definitionsById.TryGetValue(objectiveId, out ObjectiveDefinition definition);
        return definition;
    }

    public ObjectiveStateSnapshot GetObjectiveState(string objectiveId)
    {
        return definitionsById.TryGetValue(objectiveId, out ObjectiveDefinition definition)
            ? CreateSnapshot(definition, FindState(objectiveId))
            : null;
    }

    public ObjectiveDefinition GetCurrentRecommendation()
    {
        foreach (ObjectiveDefinition definition in definitions)
        {
            ObjectiveProgressData state = FindState(definition.id);
            if (state == null || state.rewardClaimed)
                continue;
            if (string.IsNullOrWhiteSpace(definition.prerequisiteObjectiveId))
                return definition;
            ObjectiveProgressData prerequisite = FindState(definition.prerequisiteObjectiveId);
            if (prerequisite != null && prerequisite.rewardClaimed)
                return definition;
        }
        return null;
    }

    private ObjectiveProgressData FindState(string objectiveId)
    {
        return gameData?.objectiveProgress?.Find(state =>
            state != null && string.Equals(state.objectiveId, objectiveId, StringComparison.Ordinal));
    }

    private static ObjectiveProgressData CreateCleanState(ObjectiveDefinition definition)
    {
        ObjectiveProgressData state = new ObjectiveProgressData
        {
            objectiveId = definition.id,
            processedEventIds = new List<string>()
        };
        ReconcileWithExistingGameState(definition, state);
        return state;
    }

    /// <summary>
    /// Most objective types are satisfied purely by replaying domain events, so a clean state is
    /// correctly incomplete. A machine-license objective is different: it describes a persistent
    /// piece of game state (owning the license) rather than a one-time transaction. That state is
    /// not reverted by <see cref="ResetForNewGame"/> on its own. A migrated save can already hold
    /// a license for a machine the objective system didn't track yet, so a machine the player
    /// already owns will never fire another
    /// "license purchased" domain event. Without this reconciliation the objective would be stuck
    /// incomplete forever, since re-purchasing an already-owned license is rejected outright. A
    /// freshly created state is therefore reconciled against the current license state at creation
    /// time: already-owned licenses complete the objective immediately (claimable, but not
    /// auto-claimed), and licenses that were also revoked (e.g. by a full license reset) correctly
    /// leave it incomplete.
    /// </summary>
    private static void ReconcileWithExistingGameState(ObjectiveDefinition definition, ObjectiveProgressData state)
    {
        if (definition.type != ObjectiveTypes.MachineLicense)
            return;
        if (!FactoryRegistry.Instance.IsMachineUnlocked(definition.targetId))
            return;

        state.progress = definition.targetValue;
        state.completed = true;
    }

    private static void Notify(Action notification, string eventName)
    {
        Delegate[] subscribers = notification?.GetInvocationList();
        if (subscribers == null)
            return;
        foreach (Action subscriber in subscribers)
        {
            try
            {
                subscriber();
            }
            catch (Exception exception)
            {
                GameLogger.LogError(LoggingManager.LogCategory.Debug,
                    $"Progression listener '{eventName}' failed: {exception.Message}");
            }
        }
    }

    private static void Notify(
        Action<ObjectiveStateSnapshot> notification,
        ObjectiveStateSnapshot snapshot,
        string eventName)
    {
        Delegate[] subscribers = notification?.GetInvocationList();
        if (subscribers == null)
            return;
        foreach (Action<ObjectiveStateSnapshot> subscriber in subscribers)
        {
            try
            {
                subscriber(snapshot);
            }
            catch (Exception exception)
            {
                GameLogger.LogError(LoggingManager.LogCategory.Debug,
                    $"Progression listener '{eventName}' failed: {exception.Message}");
            }
        }
    }

    private ObjectiveStateSnapshot CreateSnapshot(
        ObjectiveDefinition definition,
        ObjectiveProgressData state)
    {
        state ??= CreateCleanState(definition);
        bool prerequisiteClaimed = string.IsNullOrWhiteSpace(definition.prerequisiteObjectiveId) ||
            FindState(definition.prerequisiteObjectiveId)?.rewardClaimed == true;
        return new ObjectiveStateSnapshot
        {
            ObjectiveId = definition.id,
            Progress = state.progress,
            Target = definition.targetValue,
            IsActive = prerequisiteClaimed && !state.completed,
            IsCompleted = state.completed,
            IsRewardClaimed = state.rewardClaimed
        };
    }
}
