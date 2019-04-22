# MCS
This project is used to power the Avatar system known as MCS. 

## Copyright
Copyright 2015-2019 by the MCS authors and Daz 3D released under LGPLv3.

## License and Usage
This library, MCS, and all of it's original supporting code and tools are licensed under the LGPLv3 as viewed here: https://opensource.org/licenses/lgpl-3.0.html

MCS is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

MCS is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with MCS.  If not, see <https://www.gnu.org/licenses/>.

## Compiling
For ease of use there is a Visual Studio solution file MCS.sln provided which has projects inside.

1. Copy or reference your UnityEngine.dll and UnityEditor.dll (these are inside your unity install directory)
2. Build MCS_Utilities
3. Build MCS_Importer
4. Build_MCS_Core

Or you can build the individual folders with the Mono or Microsoft compiler (`mcs.exe` or `csc.exe`). You will need to build the folders in the order above and include references to them as you build the next one.


## Installing
This library is designed to be built as a series of managed dlls and then copied into your project. You can also copy the source files directly into your project. For ease of use please see the releases section on github for a `.unitypackage` which will take care of installing the dlls, shaders, and supporting editor scripts automatically for you.

You can also look at the BuildTools program which will copy the appropriate files into a structure suitable for your Unity project.

### Folder Structure
Managed plugins will exist at `Assets/MCS/Code/Plugins` including the 3rd party vendor plugin `ICSharpCode.SharpZipLib.dll`

Loose scripts will exist at `Assets/MCS/Code/Scripts` in their respective folders

Shaders will exist at `Assets/MCS/Resources/Shaders`

Content, including the base figures, will exist at `Assets/MCS/Content/[PRODUCTNAME]` and `/Assets/StreamingAssets/[VENDORNAME]`

## Support
This project is only minimially supported now, however, we are accepting pull requests and would consider having someone externally help contribute or maintain this project. If you are interested in assiting please email jjanzer at make taffi dot com.

## Legacy Assets
Please note that your existing assets will not work unless you move them into the `Assets/MCS` and update their `.mon` files to point to these new paths. If you would like to use this latest code version with legacy assets you will need to find and replace any `M3D` references with `MCS`. These references will be found in the following file types: `.mon`, `.fbx`, `.json`, and `.mr`. Be careful if you replace those strings as `fbx` and `mr` are binary files, so you should ensure you use a binary safe tool and not a string based tool. We have converted most of these assest to the new name which you can access via one of the links below.

### Previously Purchased Assets on the MCS Store
If you've purchased existing assets on the non-unity store in the past you may be able to download them via: [mcsdownload.com](https://mcsdownload.com) with your legacy username and paasword.

*Note*: this download site will expire July 2019, so if you'd like to download your assets and haven't done so yet, please do so before then.

### Purchasing New Assets
If you wanted to buy an asset but never purchased it you can find a assets for purchase at [daz3d](https://daz3d.com/shop).

## Contributors
The current maintainer of MCS is Jesse Janzer. In the past the following people have contributed development towards this project: Jesse Janzer, Ben Hesson, Bruce Stagbrook de Claimont, Abhishek Tripathi, Jesse Gomez, and Ian King.