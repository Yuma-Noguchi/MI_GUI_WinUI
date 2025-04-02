# Action Service Simplification Plan

## Current Issues
1. Over-complex handling of duplicate names
2. Separate mappings for names and IDs
3. Complex error handling for name conflicts

## Simplified Approach

### 1. Data Structure

Simple JSON structure:
```json
{
  "uuid-1": {
    "name": "Action 1",
    "class": "ds4_gamepad",
    "method": "chain",
    "args": [...]
  },
  "uuid-2": {
    "name": "Action 2",
    "class": "ds4_gamepad",
    "method": "chain",
    "args": [...]
  }
}
```

### 2. Action Save Logic

When saving an action:
1. If it's a new action (no ID), generate ID and save
2. If it's an existing action (has ID), update at that ID
3. If name matches another action, update that action instead
4. No exceptions for duplicate names - just update existing

Example flow:
```
Save "My Action"
↓
Exists with same name?
  Yes → Update that action
  No → Is new action?
       Yes → Generate ID and save
       No → Update at current ID
```

### 3. Benefits

1. **Simpler Logic**:
   - No need for name-to-id mapping
   - No duplicate name exceptions
   - Natural update behavior

2. **Better UX**:
   - Using same name updates existing action
   - No error messages for name conflicts
   - Intuitive behavior similar to file saves

3. **More Maintainable**:
   - Less code
   - Fewer edge cases
   - Clearer intent

### 4. Implementation Plan

1. Update ActionService:
   - Remove name-to-id mapping
   - Simplify save logic
   - Remove duplicate name checks

2. Update ActionData:
   - Remove metadata structure
   - Keep basic properties only

3. Update ViewModel:
   - Remove error handling for duplicates
   - Update UI directly through bindings

### 5. Migration

1. No migration needed - structure is backward compatible
2. Just need to update save/load logic

This simpler approach treats actions like files - saving with the same name updates the existing one, which is a more intuitive behavior for users.