# Magnetic-Raw-Data-Viewer
A simple data visualizer using .NET 4.5 framework to quickly browse through magnetic raw data recorded by EGS Maglog

You can double click the Chart Area, Click the Open raw file button or drag file onto the window to open raw file.

Chart Display parameters can be defined by the drop-down boxes on the right-hand side. Mag interval is the Y-axis grid interval. Fix range is the total fix number across the chart (X-axis). Data – the red line will be automatically centered to chart area using median magnetic field of displayed data.

Several shortcuts were defined to browse the data. 
```
WASD keys will scroll the chart Up, Left, Down Right without auto center the data to chart.
[ ] keys or Mouse wheel will scroll left or right by 1 fix interval.
Page Up or Page Down will scroll the chart left or right by page.
Home or End will scroll the chart to beginning or end.
```

Generate screen dumps: 
Capture screen dumps (whole file) with current Chart display parameters and dimension and save them as Gif images to a folder on Desktop.

Capture current view: 
Capture only current view on screen as Gif image and save to a folder on Desktop

Multiple files mode: 
Opening or Dragging In multiple files enable batch Generate screen dumps of those selected files. Current chart display parameters will be used for generating these Gif images.
