# Exploder Application Test Plan

## Test Environment
- Windows 10/11
- .NET 9.0 Runtime
- Visual Studio 2022 (for development)

## Test Categories

### 1. Application Startup Tests
- [ ] Application launches successfully
- [ ] Splash screen displays correctly
- [ ] Project opening dialog appears
- [ ] Recent projects list loads (if any exist)
- [ ] New project creation works
- [ ] Project opening from file works

### 2. Project Creation Tests
- [ ] New project dialog displays all options
- [ ] Project name validation works
- [ ] Project folder selection works
- [ ] Page size selection (A4, Letter, etc.)
- [ ] Orientation selection (Portrait/Landscape)
- [ ] Margin size configuration
- [ ] Background color selection
- [ ] Grid and ruler options
- [ ] Project saves correctly

### 3. User Interface Tests
- [ ] Main window displays correctly
- [ ] Menu bar is functional
- [ ] Toolbar displays all tools
- [ ] Status bar shows correct information
- [ ] Project info bar displays project name and current page
- [ ] Mode switching works (View/Insert/Edit)
- [ ] All drawing tools are enabled and functional

### 4. Drawing Tools Tests
- [ ] Circle tool creates circular objects
- [ ] Rectangle tool creates rectangular objects
- [ ] Rounded Rectangle tool creates rounded rectangles
- [ ] Triangle tool creates triangular objects
- [ ] Line tool creates lines
- [ ] Text tool creates text objects
- [ ] Image tool loads images from files
- [ ] Objects are placed correctly on canvas
- [ ] Object properties dialog appears after creation

### 5. Object Properties Tests
- [ ] Object properties dialog opens correctly
- [ ] Fill color selection works
- [ ] Border color selection works
- [ ] Border width selection works
- [ ] Text content editing works
- [ ] Font family selection works
- [ ] Font size selection works
- [ ] Link type selection works
- [ ] Link target configuration works
- [ ] File browsing for documents works
- [ ] Properties are saved correctly

### 6. Object Interaction Tests
- [ ] Object selection works
- [ ] Object deletion works
- [ ] Copy/paste functionality works
- [ ] Keyboard shortcuts work (Ctrl+C, Ctrl+V, Delete)
- [ ] Object right-click context menu works
- [ ] Object properties can be edited after creation

### 7. Navigation Tests
- [ ] View mode allows clicking objects to navigate
- [ ] Page navigation works correctly
- [ ] Back button returns to previous page
- [ ] Main button returns to main page
- [ ] Page history is maintained correctly
- [ ] Breadcrumb navigation displays correctly

### 8. Linking Tests
- [ ] Objects can link to new pages
- [ ] Objects can link to existing pages
- [ ] Objects can link to URLs
- [ ] Objects can link to documents (PDF, Word, Excel, Video)
- [ ] Document links open files correctly
- [ ] URL links open in browser
- [ ] Page links navigate correctly

### 9. File Operations Tests
- [ ] Save project works
- [ ] Save As works
- [ ] Open project works
- [ ] Recent projects list is maintained
- [ ] Project files are created in correct format
- [ ] Project files can be loaded correctly

### 10. Publishing Tests
- [ ] Publish menu item works
- [ ] Published project file creation works
- [ ] ZIP package creation works
- [ ] Published files contain all project data
- [ ] HTML viewer is generated correctly
- [ ] Self-executing package works

### 11. Template Tests
- [ ] Template service is functional
- [ ] Default templates can be created
- [ ] Templates can be saved
- [ ] Templates can be loaded
- [ ] Template projects have correct structure

### 12. Error Handling Tests
- [ ] Invalid file paths are handled gracefully
- [ ] Missing files show appropriate error messages
- [ ] Invalid project files are handled correctly
- [ ] Application doesn't crash on invalid input
- [ ] Error messages are user-friendly

### 13. Performance Tests
- [ ] Application starts quickly
- [ ] Large projects load in reasonable time
- [ ] Object creation is responsive
- [ ] Navigation is smooth
- [ ] Memory usage is reasonable

### 14. Integration Tests
- [ ] All services work together correctly
- [ ] Data flows correctly between components
- [ ] UI updates reflect data changes
- [ ] Commands execute correctly
- [ ] Undo/Redo structure is in place

## Test Execution Steps

### Manual Testing
1. Launch application
2. Create new project
3. Test each drawing tool
4. Test object properties
5. Test navigation
6. Test file operations
7. Test publishing
8. Test error scenarios

### Automated Testing (Future)
- Unit tests for services
- Integration tests for UI components
- End-to-end tests for complete workflows

## Expected Results
- All features work as specified in requirements
- UI is responsive and user-friendly
- Data is persisted correctly
- Error handling is robust
- Performance is acceptable

## Test Report
After running tests, document:
- Passed tests
- Failed tests
- Performance metrics
- Issues found
- Recommendations for improvement 