# Exploder - Interactive Documentation System

## Overview

Exploder is a top-down documentation program designed for creating interactive documentation with multiple layers. It provides capabilities similar to PowerPoint but is specifically tailored for technical documentation applications.

## Features

### Core Functionality
- **Interactive Documentation**: Create multi-layered documentation with clickable objects
- **Object Types**: Circles, rectangles, triangles, lines, text, images, and URLs
- **Navigation**: Click objects to navigate to new pages or open documents
- **Transparent Objects**: Create highlight effects with transparent fills
- **Text Overlay**: Add text to any shape or object
- **Resizable Images**: Resize images using corner handles in edit mode

### File Support
- **Documents**: PDF, Word, Excel, Video files
- **Images**: JPG, PNG, GIF, BMP
- **Links**: URLs, internal page navigation
- **Excel Integration**: Select specific cell ranges from Excel files

### Project Management
- **Project Creation**: Create new projects with custom page sizes and orientations
- **Save/Load**: Save projects as .exp files with automatic backup
- **Publish**: Create self-executing packages for distribution
- **Print**: Print pages with all objects and formatting

## Getting Started

### Installation
1. Ensure you have .NET 9.0 or later installed
2. Download and extract the Exploder application
3. Run `Exploder.exe` to start the application

### Creating Your First Project
1. **Start the Application**: Launch Exploder
2. **Create New Project**: Click "New" and enter project details
3. **Add Objects**: Use the toolbar to add shapes, text, and images
4. **Set Properties**: Right-click objects to configure links and properties
5. **Navigate**: Click objects to test navigation and document links

## User Interface

### Main Window
- **Menu Bar**: File operations, design tools, and help
- **Toolbar**: Quick access to drawing tools and modes
- **Canvas**: Main drawing area where you place objects
- **Status Bar**: Shows current mode and object count

### Modes
- **View Mode**: Click objects to navigate (default)
- **Insert Mode**: Add new objects to the page
- **Edit Mode**: Move, resize, and modify existing objects

### Toolbar Tools
- **Shapes**: Circle, Rectangle, Rounded Rectangle, Triangle, Line
- **Content**: Text, URL, Image
- **Edit**: Undo, Redo, Copy, Paste, Delete
- **Navigation**: Back to Previous, Back to Main

## Object Properties

### Basic Properties
- **Name**: Object identifier
- **Position**: X-Y coordinates on the page
- **Size**: Width and height dimensions
- **Colors**: Fill color, stroke color, transparency
- **Opacity**: Overall transparency level

### Link Properties
- **Link Type**: None, Page, URL, Document, Excel Data
- **Target**: Page name, URL, or file path
- **File Type**: PDF, Word, Excel, Video, Image

### Text Properties
- **Content**: Text to display
- **Font**: Family, size, weight
- **Color**: Text color
- **Alignment**: Text positioning

## File Menu

### New Project
- Choose page size (A4, Letter, Custom)
- Set orientation (Portrait/Landscape)
- Configure margins and background

### Open/Save
- Open existing .exp project files
- Save projects with automatic backup
- Save As to different locations

### Print
- Print current page with all objects
- Includes project and page information
- Maintains object positioning and formatting

### Publish
- Create self-executing package
- Includes all linked documents
- Portable distribution format

## Design Menu

### Objects
- Access to all available object types
- Quick object placement tools

### Standard Documents
- **PDF Document**: Add PDF file links
- **Word Document**: Add Word file links
- **Excel Spreadsheet**: Add Excel file links with range selection
- **Video File**: Add video file links
- **Image File**: Add image file links

## Help Menu

### About
- Program version information
- Copyright and licensing details

### Help
- This documentation
- User guide and tutorials

## Keyboard Shortcuts

- **Ctrl+N**: New Project
- **Ctrl+O**: Open Project
- **Ctrl+S**: Save Project
- **Ctrl+Shift+S**: Save As
- **Ctrl+P**: Print
- **Ctrl+Z**: Undo
- **Ctrl+Y**: Redo
- **Ctrl+C**: Copy
- **Ctrl+V**: Paste
- **Delete**: Delete selected object

## Tips and Best Practices

### Object Organization
- Use descriptive names for objects
- Group related objects together
- Use consistent colors and styles

### Navigation Design
- Create clear navigation paths
- Use intuitive object names
- Test navigation flow thoroughly

### File Management
- Keep linked files in organized folders
- Use relative paths when possible
- Regularly save and backup projects

### Performance
- Optimize image sizes for better performance
- Limit the number of objects per page
- Use appropriate file formats for documents

## Troubleshooting

### Common Issues

**Object not responding to clicks**
- Ensure you're in View mode
- Check that the object has a link configured
- Verify the target file/page exists

**Images not displaying**
- Check file path is correct
- Ensure image format is supported
- Verify file permissions

**Print not working**
- Check printer is connected and online
- Verify page size settings
- Ensure objects are within print margins

**Excel links not working**
- Verify Excel file path is correct
- Check cell range syntax (e.g., A1:B10)
- Ensure Excel is installed on target system

### Support
For additional support or feature requests, please refer to the project documentation or contact the development team.

## Version History

### Version 1.0 (Current)
- Initial release with core functionality
- Basic object types and navigation
- File linking and document support
- Print and publish capabilities
- Excel integration with range selection

## Technical Requirements

- **Operating System**: Windows 10 or later
- **Framework**: .NET 9.0
- **Memory**: 4GB RAM minimum, 8GB recommended
- **Storage**: 100MB for application, additional space for projects
- **Display**: 1024x768 minimum resolution

## License

This software is provided as-is for educational and development purposes. Please refer to the license file for complete terms and conditions. 