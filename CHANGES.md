# Changes Summary

## Interactive Variables Enhancement

This document summarizes the changes made to add interactive variables support to the Flow Launcher Snippets plugin.

### Files Modified

#### 1. Core Logic Files
- **`Main.cs`**: Updated Query method to support variable parsing and replacement
- **`Util/VariableHelper.cs`**: New utility class for variable operations

#### 2. Documentation Files
- **`README.md`**: Added interactive variables section
- **`VARIABLES_GUIDE.md`**: Comprehensive guide for the new feature

### Features Added

#### Variable Processing
- Parse variables from snippet templates using `{variable_name}` syntax
- Parse runtime arguments in `variable=value` format
- Replace variables with provided values
- Handle missing variables with helpful error messages

#### User Experience
- Automatic help suggestions for missing variables
- Higher priority for completed variable substitutions
- Backward compatibility with existing snippets
- English-only code comments and documentation

#### Technical Implementation
- Regular expression-based variable detection
- Dictionary-based variable storage and lookup
- Type-safe variable information tracking
- Comprehensive error handling

### Code Quality Improvements
- All code comments converted to English
- Consistent documentation format
- Clear separation of concerns
- Maintainable and extensible architecture

### Testing
- Build successfully without errors
- All existing functionality preserved
- New features tested and working
- Ready for production deployment

### Deployment
- Updated DLL file: `Flow.Launcher.Plugin.Snippets.dll`

---

## Advanced Scoring System & Usage Statistics

### Overview
Implemented a comprehensive scoring system that dynamically calculates snippet relevance based on multiple factors including match quality, usage frequency, recency, variable completeness, complexity, and user favorites.

### Files Modified

#### 1. Data Model Enhancement
- **`SnippetModel.cs`**: Extended model with usage tracking and favorites
  - Added `UsageCount`: Tracks number of times snippet is used
  - Added `LastUsedTime`: Records timestamp of last usage
  - Added `IsFavorite`: Boolean flag for user favorites
  - Updated `ToString()` method to include new properties

#### 2. New Scoring Engine
- **`Util/ScoreCalculator.cs`**: New comprehensive scoring calculation engine
  - **Match Quality (0-20 points)**:
    - Exact match: 20 points
    - Starts with query: 15 points
    - Contains query: 10 points
    - Fuzzy match: 5 points
  - **Usage Frequency (0-15 points)**:
    - 10+ uses: 15 points
    - 5+ uses: 10 points
    - 1+ uses: 5 points
  - **Recency Bonus (0-10 points)**:
    - Within last hour: 10 points
    - Within last day: 7 points
    - Within last week: 4 points
    - Within last month: 2 points
  - **Variable Completeness (-5 to +10 points)**:
    - All variables provided: +10 points
    - Missing variables: -5 points
  - **Complexity Penalty (0 to -5 points)**:
    - Based on number of variables (more variables = higher penalty)
  - **Favorite Boost (+25 points)**:
    - Marked favorites get significant boost

#### 3. Core Integration
- **`Main.cs`**: Integrated scoring system into query flow
  - Updated `Query()` method to calculate enhanced scores using `ScoreCalculator`
  - Modified `_modelToResult()` to accept and display enhanced scores
  - Added `_buildScoreInfo()` helper to format score breakdown information
  - Implemented `_updateUsageStats()` to automatically track usage
  - Updated result subtitles to show:
    - Calculated score with base score
    - Usage count (e.g., "Used: 5x")
    - Last used time (human-readable format like "Last: 2 hours ago")
  - Added ⭐ marker for favorite snippets in titles
  - Added ✓ marker for completed variable assignments
  - Updated `_createVariableResult()` and `_createVariableHelpResult()` for consistency

#### 4. Persistence Layer Updates
- **`Json/JsonSettingSnippetManage.cs`**: JSON storage enhancements
  - Updated `Add()` method to preserve usage statistics
  - Updated `UpdateByKey()` to maintain UsageCount, LastUsedTime, and IsFavorite
  - Added `ResetAllScore()` method to clear scores and usage stats (preserves favorites)
  
- **`Sqlite/SqliteSnippetManage.cs`**: SQLite storage enhancements
  - Extended table schema with new columns:
    - `usage_count INTEGER DEFAULT 0`
    - `last_used_time TEXT`
    - `is_favorite INTEGER DEFAULT 0`
  - Implemented automatic migration in `_initCheckTable()` to add columns to existing databases
  - Updated `_readSnippetModel()` to read new columns
  - Modified `Add()` and `UpdateByKey()` to include new fields
  - Updated `ResetAllScore()` to clear scores and usage stats

#### 5. UI Enhancements
- **`SnippetDialog.xaml` & `SnippetDialog.xaml.cs`**: Add/Edit dialog improvements
  - Added "Add to Favorites" checkbox to snippet creation/editing
  - Updated save logic to persist `IsFavorite` flag
  - Preserved usage statistics when updating existing snippets
  
- **`FormWindows.xaml` & `FormWindows.xaml.cs`**: Management window improvements
  - Added `UsageCount` column to DataGrid
  - Added `Favorite` column with ⭐ visual indicator
  - Enhanced edit panel with usage statistics display:
    - `TxtUsageCount`: Shows number of times used
    - `TxtLastUsed`: Shows human-readable last used time (e.g., "2 hours ago", "Never")
  - Updated `_renderSelect()` to populate usage information
  - Preserved usage statistics when saving manual edits
  - Improved data binding for better responsiveness

#### 6. Context Menu Integration
- **`Main.cs`**: Added context menu action
  - "Toggle Favorite" option to quickly mark/unmark snippets as favorites
  - Updates IsFavorite flag and persists change immediately

### Features Added

#### Dynamic Scoring
- Multi-factor relevance calculation combining 6 different scoring components
- Automatic score adjustment based on user behavior
- Real-time score updates as snippets are used
- Transparent score breakdown visible in search results

#### Usage Tracking
- Automatic increment of usage count on each snippet execution
- Timestamp recording for recency calculations
- Persistent storage across sessions
- No manual intervention required

#### Favorites System
- User-controlled favorite marking
- Significant score boost for favorites (+25 points)
- Visual ⭐ indicator in search results and management UI
- Quick toggle via context menu

#### Enhanced Search Results
- Detailed subtitle showing:
  - Score breakdown: `[Score: X Base: Y]`
  - Usage statistics: `[Used: Nx]`
  - Recency information: `[Last: X ago]`
- Visual indicators (⭐ for favorites, ✓ for completed variables)
- Better result ordering based on comprehensive scoring

#### Management UI Improvements
- Usage statistics visible in snippet list
- Favorite column with star indicator
- Edit panel displays usage count and last used time
- Manual score editing capability preserved
- Automatic preservation of statistics during edits

### Technical Implementation

#### Migration & Backward Compatibility
- Automatic SQLite schema migration for existing databases
- Safe column addition without data loss
- Default values for new fields ensure compatibility
- JSON storage automatically accommodates new properties

#### Code Quality
- Centralized scoring logic in dedicated `ScoreCalculator` class
- Separation of concerns between calculation and presentation
- Comprehensive helper methods for score component calculations
- Maintainable and testable architecture

#### Error Handling
- Safe handling of nullable timestamps
- Graceful degradation when statistics are unavailable
- Proper default values for new installations

### Testing & Validation
- Built successfully with Debug and Release configurations
- Published to Flow Launcher plugin directory
- All existing functionality preserved
- New features tested and operational
- No breaking changes to existing snippets

### Deployment
- Updated plugin DLL deployed to `%APPDATA%\FlowLauncher\Plugins\Snippets-2.2.0\`
- Flow Launcher process managed for successful file replacement
- Ready for immediate use
- Compatible with existing Flow Launcher installations
- No breaking changes to existing snippets

---

## Compact Score Display & Resizable UI

### Overview
Optimized score information display to be more compact and improved UI layout to prevent text truncation.

### Problem Addressed
1. **Long Subtitles in Flow Launcher**: Score information was too verbose, requiring very wide window to see complete information
2. **Cut-off Text in FormWindows**: "Last used" information was truncated due to fixed field widths and non-resizable window

### Files Modified

#### 1. Compact Score Format
- **`Main.cs`**: Redesigned `_buildScoreInfo()` method for compact display
  - Changed from verbose format: `[Score: 45 Base: 20] [Used: 5x] [Last: 2 hours ago]`
  - To compact format with icons: `⚡45 ↻5 🕐2h`
  - Score display: `⚡` (lightning) + score number
  - Usage count: `↻` (circular arrow) + count
  - Last used time: `🕐` (clock) + abbreviated time
  - Time abbreviations:
    - Minutes: `Xm` (e.g., `15m`)
    - Hours: `Xh` (e.g., `3h`)
    - Days: `Xd` (e.g., `5d`)
    - Weeks: `Xw` (e.g., `2w`)
    - Months: `Xmo` (e.g., `3mo`)
  - Empty string when no statistics available (cleaner display)

#### 2. Resizable Management Window
- **`FormWindows.xaml`**: Made window resizable with proper constraints
  - Changed `ResizeMode` from `NoResize` to `CanResize`
  - Maintained default size: `Width="1200"` `Height="640"`
  - Added minimum size constraints:
    - `MinWidth="900"` - Ensures usability on smaller screens
    - `MinHeight="500"` - Prevents toolbar/controls overlap
  - Users can now resize window to fit their screen and preference

#### 3. Improved Field Layout
- **`FormWindows.xaml`**: Enhanced usage statistics display in edit panel
  - Increased "Last Used" field width from `110` to `200` pixels
  - Added `TextWrapping="Wrap"` to prevent text cut-off
  - Ensures full timestamp visibility without truncation
  - Better accommodation for longer date/time formats

### Benefits

#### User Experience
- **Cleaner Flow Launcher Results**: Much shorter subtitle text fits in narrower windows
- **Better Readability**: Icon-based display is visually cleaner and easier to scan
- **Flexible Window Size**: Users can adjust FormWindows to their preference
- **No More Truncation**: All information fully visible without horizontal scrolling

#### Technical Improvements
- Reduced string length by ~60-70% in typical cases
- Maintains all essential information
- Locale-independent icons (no translation needed)
- Better visual hierarchy with icon markers

### Examples

#### Before & After Comparison

**Before (Verbose)**:
```
sp tmux a=session b=window
[Score: 45 Base: 20] [Used: 5x] [Last: 2 hours ago]
```

**After (Compact)**:
```
sp tmux a=session b=window
⚡45 ↻5 🕐2h
```

### Testing & Validation
- Built successfully with no errors
- Plugin deployed to Flow Launcher plugins directory
- Tested with various snippet lengths
- Window resizing works smoothly
- All text fields display fully without truncation

### Deployment
- Updated DLL deployed to Flow Launcher plugin directory
- Flow Launcher process stopped and restarted for file replacement
- Changes immediately visible upon restart