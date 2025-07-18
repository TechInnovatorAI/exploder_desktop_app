# Exploder Test Plan

## Copy, Cut, and Paste Functionality Testing

### Test Cases

#### 1. Copy Functionality
- **Test Case**: Copy a selected object
- **Steps**:
  1. Create a new project
  2. Add a shape (circle, rectangle, etc.) to the canvas
  3. Select the shape by clicking on it
  4. Click the copy button (📋) or press Ctrl+C
  5. Verify status message shows "Object copied to clipboard"
  6. Verify the original object remains unchanged

#### 2. Cut Functionality
- **Test Case**: Cut a selected object
- **Steps**:
  1. Create a new project
  2. Add a shape to the canvas
  3. Select the shape by clicking on it
  4. Click the cut button (✂) or press Ctrl+X
  5. Verify status message shows "Object cut to clipboard"
  6. Verify the original object is removed from the canvas
  7. Verify object count decreases

#### 3. Paste Functionality
- **Test Case**: Paste a copied/cut object
- **Steps**:
  1. Copy or cut an object (from previous tests)
  2. Click the paste button (📄) or press Ctrl+V
  3. Verify status message shows "Object pasted"
  4. Verify a new object appears on the canvas
  5. Verify the new object is offset from the original position
  6. Verify object count increases

#### 4. Undo/Redo with Copy/Cut/Paste
- **Test Case**: Undo and redo copy/cut/paste operations
- **Steps**:
  1. Perform a cut operation
  2. Press Ctrl+Z to undo
  3. Verify the cut object is restored
  4. Press Ctrl+Y to redo
  5. Verify the cut operation is performed again

#### 5. Keyboard Shortcuts
- **Test Case**: Verify all keyboard shortcuts work
- **Steps**:
  1. Test Ctrl+C (Copy)
  2. Test Ctrl+X (Cut)
  3. Test Ctrl+V (Paste)
  4. Test Ctrl+Z (Undo)
  5. Test Ctrl+Y (Redo)
  6. Test Delete key (Delete)

#### 6. Edge Cases
- **Test Case**: Copy/Cut/Paste with no selection
- **Steps**:
  1. Try to copy without selecting an object
  2. Verify status message shows "No object selected to copy"
  3. Try to cut without selecting an object
  4. Verify status message shows "No object selected to cut"
  5. Try to paste without anything in clipboard
  6. Verify status message shows "No object in clipboard to paste"

#### 7. Multiple Operations
- **Test Case**: Perform multiple copy/paste operations
- **Steps**:
  1. Copy an object
  2. Paste it multiple times
  3. Verify each paste creates a new object
  4. Verify undo works for each paste operation

### Expected Results

1. **Copy**: Creates a copy of the object in clipboard without modifying the original
2. **Cut**: Removes the object from the canvas and stores it in clipboard
3. **Paste**: Creates a new object from clipboard content with slight position offset
4. **Undo/Redo**: Properly restores or re-applies operations
5. **Keyboard Shortcuts**: All shortcuts work as expected
6. **Error Handling**: Appropriate error messages for invalid operations

### Notes

- All operations should work in Edit mode
- Objects should maintain their properties (colors, text, links, etc.) when copied/cut/pasted
- The command pattern ensures proper undo/redo functionality
- Clipboard content persists until overwritten by another copy/cut operation 