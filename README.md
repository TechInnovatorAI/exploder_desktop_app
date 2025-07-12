# Exploder - Top-Down Documentation Program

## Overview

Exploder is a Windows-based documentation program designed for creating hierarchical, interactive documentation for complex items with multiple layers of information. It's similar to PowerPoint but specifically tailored for technical documentation, machine documentation, organizational charts, and business processes.

## Features

### Core Functionality
- **Hierarchical Navigation**: Create multi-level documentation with clickable objects that navigate to detailed pages
- **Multiple Object Types**: Support for circles, rectangles, rounded rectangles, triangles, lines, text, and images
- **Interactive Links**: Objects can link to new pages, documents (PDF, Word, Excel, Video), URLs, or Excel data
- **Page Management**: Create, navigate, and manage multiple pages within a project
- **Project Persistence**: Save and load projects with automatic backup functionality

### User Interface
- **Three Operating Modes**:
  - **View Mode**: Navigate through documentation by clicking objects
  - **Insert Mode**: Add new objects to pages
  - **Edit Mode**: Modify existing objects and properties
- **Modern UI**: Clean, professional interface with gradient backgrounds and intuitive controls
- **Toolbar**: Quick access to drawing tools, edit functions, and navigation
- **Status Bar**: Real-time information about project status, object count, and mouse position

### Drawing Tools
- **Circle Tool**: Create circular objects
- **Rectangle Tool**: Create rectangular objects
- **Rounded Rectangle Tool**: Create rectangles with rounded corners
- **Triangle Tool**: Create triangular objects
- **Line Tool**: Create lines and arrows
- **Text Tool**: Add text objects with customizable fonts
- **Image Tool**: Insert images from files

### Object Properties
- **Visual Properties**: Fill color, border color, border thickness, opacity
- **Text Properties**: Font family, font size, text content
- **Link Properties**: Link type (None, Page, URL, Document), target specification
- **Position Properties**: X/Y coordinates, width, height, Z-index

### File Operations
- **New Project**: Create projects with customizable page settings
- **Open Project**: Load existing projects from file
- **Save/Save As**: Save projects with automatic backup
- **Recent Projects**: Quick access to recently opened projects (up to 10)
- **Publishing**: Create self-executing packages or published project files

### Advanced Features
- **Copy/Paste**: Duplicate objects with keyboard shortcuts (Ctrl+C, Ctrl+V)
- **Undo/Redo**: Command pattern implementation for editing operations
- **Object Selection**: Click to select objects for editing
- **Page Navigation**: Navigate between pages with breadcrumb trail
- **Template System**: Built-in templates for common documentation types

## Installation

### Prerequisites
- Windows 10 or later
- .NET 9.0 Runtime
- Visual Studio 2022 (for development)

### Building from Source
1. Clone the repository
2. Open `Exploder.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Build the solution
5. Run the application

### Running the Application
1. Launch `Exploder.exe`
2. The application will start with a splash screen
3. Choose to create a new project or open an existing one
4. Begin creating your documentation

## Usage Guide

### Creating a New Project
1. Click "New Project" in the startup dialog
2. Enter a project name
3. Select a project folder
4. Choose page settings:
   - Page size (A4, Letter, etc.)
   - Orientation (Portrait/Landscape)
   - Margin size
   - Background color
   - Grid and ruler options
5. Click "OK" to create the project

### Adding Objects
1. Switch to "Insert" mode using the toolbar button
2. Select a drawing tool (circle, rectangle, etc.)
3. Click and drag on the canvas to create the object
4. The object properties dialog will appear
5. Configure the object's properties and links
6. Click "OK" to add the object

### Configuring Object Properties
- **Basic Properties**: Name, fill color, border color, border thickness
- **Text Properties**: Text content, font family, font size
- **Link Properties**: 
  - **None**: Object has no action when clicked
  - **Page**: Navigate to a new or existing page
  - **URL**: Open a web URL
  - **Document**: Open a file (PDF, Word, Excel, Video)
  - **Excel Data**: Link to specific Excel cells

### Navigating Documentation
1. Switch to "View" mode
2. Click on objects to navigate to their linked destinations
3. Use "Back" button to return to previous page
4. Use "Main" button to return to the main page
5. Page breadcrumbs show your current location

### Publishing Projects
1. Go to File → Publish
2. Choose output format:
   - **Exploder Project (.exp)**: Standard project file
   - **ZIP Package (.zip)**: Self-executing package with HTML viewer
3. Select output location
4. Click "Save" to create the published version

## Project Structure

```
Exploder/
├── App.xaml                 # Application entry point
├── App.xaml.cs              # Application logic
├── Commands/                # Command pattern implementation
│   ├── ICommand.cs          # Command interface
│   ├── AddObjectCommand.cs  # Add object command
│   └── DeleteObjectCommand.cs # Delete object command
├── Infrastructure/          # Infrastructure components
│   ├── Assets/              # Application assets
│   └── Setting/             # Application settings
│       ├── AppMode.cs       # Application modes
│       └── SerializedElement.cs # Serialization support
├── Models/                  # Data models
│   ├── ProjectData.cs       # Project data structure
│   └── PointData.cs         # Point data structure
├── Services/                # Business logic services
│   ├── IProjectService.cs   # Project service interface
│   ├── ProjectService.cs    # Project service implementation
│   ├── IPublishingService.cs # Publishing service interface
│   ├── PublishingService.cs # Publishing service implementation
│   ├── ITemplateService.cs  # Template service interface
│   └── TemplateService.cs   # Template service implementation
├── Views/                   # User interface views
│   ├── MainWindow.xaml      # Main application window
│   ├── MainWindow.xaml.cs   # Main window logic
│   ├── ProjectOpenWindow.xaml # Project opening dialog
│   ├── ProjectOpenWindow.xaml.cs # Project opening logic
│   ├── ObjectPropertiesWindow.xaml # Object properties dialog
│   ├── ObjectPropertiesWindow.xaml.cs # Object properties logic
│   ├── SplashWindow.xaml    # Splash screen
│   └── SplashWindow.xaml.cs # Splash screen logic
└── ViewModels/              # View models (future MVVM implementation)
```

## Architecture

### Design Patterns
- **Command Pattern**: Used for undo/redo operations
- **Service Pattern**: Business logic separated into services
- **Model-View Separation**: Clear separation between data and presentation

### Key Components
- **ProjectData**: Central data model containing all project information
- **ExploderObject**: Represents individual objects on pages
- **PageData**: Represents individual pages within a project
- **Services**: Handle business logic for projects, publishing, and templates

## Templates

The application includes built-in templates for common documentation types:

### Machine Documentation Template
- Designed for documenting machinery with multiple components
- Includes sample objects for engine, transmission, and hydraulics
- Landscape orientation with grid for technical drawings

### Organizational Chart Template
- Designed for creating organizational charts
- Includes sample objects for CEO, CTO, and CFO positions
- Portrait orientation with rounded rectangles

### Business Documentation Template
- Designed for business process documentation
- Includes sample objects for process flow, policies, procedures, and forms
- Portrait orientation with grid for structured layouts

## File Formats

### Project Files (.exp)
- JSON-based format for storing project data
- Contains all project information including pages, objects, and settings
- Human-readable format for easy inspection and debugging

### Published Files
- **Standard (.exp)**: Read-only project file
- **ZIP Package (.zip)**: Self-executing package with HTML viewer and assets

## Keyboard Shortcuts

- **Ctrl+N**: New project
- **Ctrl+O**: Open project
- **Ctrl+S**: Save project
- **Ctrl+Shift+S**: Save project as
- **Ctrl+C**: Copy selected object
- **Ctrl+V**: Paste object
- **Delete**: Delete selected object
- **Ctrl+Z**: Undo (planned)
- **Ctrl+Y**: Redo (planned)

## Future Enhancements

### Phase 2 Features (Planned)
- Enhanced object types (arrows, callouts, etc.)
- Video and audio playback integration
- Excel data integration with live updates
- Advanced font selection and text formatting
- Object grouping and alignment tools
- Print functionality
- Search and replace functionality
- Project structure visualization
- Database integration
- STEP file support for CAD integration

### Advanced Features (Future)
- Mobile app companion
- Cloud-based collaboration
- AI-assisted content organization
- Integration with CAD software (SolidWorks, Altium)
- Enterprise licensing system
- Advanced scripting capabilities

## Troubleshooting

### Common Issues
1. **Application won't start**: Ensure .NET 9.0 Runtime is installed
2. **Can't save projects**: Check folder permissions and available disk space
3. **Images not loading**: Verify image file paths and formats
4. **Links not working**: Ensure target files exist and paths are correct

### Performance Tips
- Use appropriate image sizes for better performance
- Limit the number of objects per page for optimal navigation
- Regularly save your work to prevent data loss
- Use the grid feature for precise object placement

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

For support and questions:
- Create an issue in the GitHub repository
- Check the documentation in the Help menu
- Review the troubleshooting section above

## Version History

### Version 1.0 (Current)
- Initial release with core functionality
- Basic object types and navigation
- Project saving and loading
- Publishing capabilities
- Template system
- Copy/paste functionality

---

**Exploder Development Team**  
© 2025 All rights reserved 