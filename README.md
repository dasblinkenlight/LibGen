# Generator of Partial Razor Views for ASP.NET Core Libraries

This is a MSBuild tool for generating partial Razor views for inclusion in your ASP.NET Core pages.

## Problem

Referencing client-side libraries backed by Content Distribution Networks (CDN) requires attention to details:
you need to copy the full path and its matching integrity hash for both the css and js files into the source code
of your Razor page, and copy the relevant files into the local wwwroot folder. You also need to maintain all the
items to upgrade to the next version of the library.

## Solution

This simple tool solves the problem by introducing a simple convention: each client-side library
maps to one or more partial Razor views containing a single script link, which is then included into your Razor view.

<details>
  <summary>Sample Partial View</summary>

```html
<script
    rel="stylesheet"
    src="https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.3/js/bootstrap.bundle.min.js"
    asp-fallback-src="/assets/vendor/bootstrap/js/bootstrap.bundle.min.js"
    asp-fallback-test="window.bootstrap"
    asp-suppress-fallback-integrity="true"
    integrity="sha512-7Pi/otdlbbCR+LnW+F7PwFcSDJOuUJB3OxtEHbg4vSMvzvJjde4Po1v4BR9Gdc9aXNUNFVUY+SK51wWT8WF0Gg=="
    crossorigin="anonymous"
    referrerpolicy="no-referrer">
</script>
```
</details>

<details>
  <summary>Sample Inclusion into ASP.NET Core page</summary>

```html
<partial name="_BootstrapCSS"/>
```
</details>

## Configuring the Tool

1. Put a configuration file `LibDef.json` into the root folder of your ASP.NET Core project
2. Add LibGen to your csproj file
3. Configure the root folder and the folder for the fallback root
4. Build your project to make local copies of your files and generate partial views
5. Include the generated partial views in your Razor pages 

<details>
  <summary>Sample Configuration File</summary>

```json
{
  "version": "1.0",
  "libraries": [
    {
      "library": "bootstrap",
      "version": "5.3.3",
      "provider": "cdnjs",
      "files": [
        {
          "file": "js/bootstrap.bundle.min.js",
          "component":"_BootstrapCSS",
          "test-class":"window.bootstrap"
        },
        {
          "file": "css/bootstrap.min.css",
          "component":"_BootstrapJS",
          "test-class":"visually-hidden",
          "test-property":"position",
          "test-value":"absolute"
        }
      ]
    },
    {
      "library": "bootstrap-icons",
      "version": "1.11.3",
      "provider": "cdnjs",
      "files": [
        {
          "file": "font/bootstrap-icons.min.css",
          "component":"_BootstrapIcons",
          "test-class":"bi",
          "test-property":"font-family",
          "test-element":":before",
          "test-value":"bootstrap-icons"
        },
        "font/fonts/bootstrap-icons.woff",
        "font/fonts/bootstrap-icons.woff2"
      ]
    }
    ]
  }
```
</details>

<details>
  <summary>Sample Project File</summary>

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <RootNamespace>My.Project.Namespace</RootNamespace>
        <!-- This setting lets you specify an override for the default LibDef.json name -->
        <LibraryDefinitionsFile>ClientLibraries.json</LibraryDefinitionsFile>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="Dasblinkenlight.LibGen" Version="1.1.0" />
    </ItemGroup>
</Project>
```
</details>

## Additional Features

For libraries with fallback that cannot be tested via the standard ASP.NET mechanisms,
this tool generates a testing script similar to what ASP.NET generates for you.
This happens transparently when you add `"test-element":"..."` to your `LibDef.json` file.

<details>
<summary>Sample Test Script</summary>

```html
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/bootstrap-icons/1.11.3/font/bootstrap-icons.min.css" integrity="sha512-dPXYcDub/aeb08c63jRq/k6GaKccl256JQy/AnOq7CAnEZ9FzSL9wSbcZkMp4R26vBsMLFYH4kQ67/bbV8XaCQ==" crossorigin="anonymous" referrerpolicy="no-referrer" /><meta name="x-stylesheet-fallback-test" content="" class="bi" /><script>
	!function(e,t,l,i,n){var u,f=document,g=f.getElementsByTagName("SCRIPT"),g=g[g.length-1].previousElementSibling,n=f.defaultView&&f.defaultView.getComputedStyle?f.defaultView.getComputedStyle(g,n):g.currentStyle;if(n&&n[e]!==t)for(u=0;u<l.length;u++)f.write('<link href="'+l[u]+'" '+i+"/>")}("font-family","bootstrap-icons",["/assets/vendor/bootstrap-icons/font/bootstrap-icons.min.css"]," rel=\u0022stylesheet\u0022 crossorigin=\u0022anonymous\u0022 referrerpolicy=\u0022no-referrer\u0022",":before");
</script>
```
</details>
