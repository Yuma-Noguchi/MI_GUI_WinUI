# Action Service Improvement Plan - Revised

## Current Issues
1. JSON structure uses redundant name storage
2. Duplicate entries when renaming actions
3. Complex handling of updates and renames

## Updated Solution Design

### 1. Action Data Structure

Keep ActionData compatible with existing code:

```csharp
public class ActionData
{
    public string Id { get; set; }  // Keep for backward compatibility
    public string Name { get; set; }
    public string Class { get; set; }  // For ds4_gamepad
    public string Method { get; set; }  // For chain
    public List<Dictionary<string, object>> Args { get; set; }
    
    // Helper methods
    public static ActionData CreateButtonPress(string button)
    public static ActionData CreateSleep(double seconds)
}
```

### 2. JSON Structure

Current (Problem):
```json
{
  "Action Name": {
    "name": "Action Name",
    "id": "uuid-1",
    "sequence": { ... }
  }
}
```

New (Solution):
```json
{
  "actions": {
    "uuid-1": {
      "name": "Action Name",
      "class": "ds4_gamepad",
      "method": "chain",
      "args": [
        { "press": ["button"] },
        { "sleep": [1.0] }
      ]
    }
  },
  "metadata": {
    "version": "2.0",
    "nameToId": {
      "Action Name": "uuid-1"
    }
  }
}
```

### 3. Implementation Strategy

1. Update ActionService:
   - Add version field to JSON
   - Keep backward compatibility for reading old format
   - Use new format for writing
   - Add migration logic

2. Maintain Compatibility:
   - Keep Id property in ActionData but use it only for serialization
   - Preserve existing helper methods and properties
   - Handle both old and new JSON formats

3. Improve Action Operations:
   - Use stable IDs as primary keys
   - Store name-to-id mapping
   - Validate names and IDs
   - Handle renames efficiently

### 4. Migration Plan

1. Version Detection:
   ```csharp
   if (json.Contains("metadata")) {
       // New format
       LoadNewFormat(json);
   } else {
       // Old format
       MigrateFromOldFormat(json);
   }
   ```

2. Data Migration:
   - Read old format
   - Convert to new structure
   - Preserve IDs and names
   - Add metadata section
   - Save in new format

### 5. Benefits

1. **Data Integrity**:
   - Consistent ID usage
   - No duplicate entries
   - Clear name-to-id mapping

2. **Backward Compatibility**:
   - Maintains existing API surface
   - Supports old JSON format
   - Preserves helper methods

3. **Better Organization**:
   - Separated metadata
   - Clear version tracking
   - Efficient lookups

4. **Future Extensibility**:
   - Versioned data format
   - Metadata section for future needs
   - Clean migration path

## Next Steps

1. Update ActionData class to maintain compatibility
2. Implement new JSON structure with metadata
3. Add migration logic to ActionService
4. Update save/load operations
5. Test with existing code

## Migration Notes

- Automatic migration on first load of old format
- Logging of migration process
- Backup of old format before migration
- Validation of migrated data