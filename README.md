# res-map

This task has the benefit of creating a map between the embedded resources of your project and their local file system paths.
Using those file system paths during debug can be used to load the files directly from the file system so that it is not 
needed to recompile your project again.

For example if you have a text file from which you are displaying some content, then you would benefit from not recompiling and 
restarting the application for the changes to take effect.


## Nuget
Install-Package ResMap


## Sample usage
```
private static Stream GetStream(string resourceName, System.Reflection.Assembly containingAssembly)
{
#if DEBUG
    string filePath = null;
    if (ResRepo.TryGetPath(containingAssembly.GetName().FullName, resourceName, out filePath))
        return new FileStream(filePath, FileMode.Open);

    return null;
#else
    return containingAssembly.GetManifestResourceStream(resourceName);
#endif
}
```
