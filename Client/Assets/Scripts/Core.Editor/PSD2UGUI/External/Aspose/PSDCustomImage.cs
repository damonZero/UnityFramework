using Aspose.PSD.FileFormats.Psd.Layers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Aspose.PSD
{
    public class PSDCustomImage : Layer
    {
        public static void Save(Layer layer, string filePath, ImageOptionsBase optionsBase)
        {
            using (FileStream fileStream = File.Create(filePath))
            {
                IImageExporter imageExporter = ImageExportersRegistry.CreateFirstSupportedExporter(layer, optionsBase);
                Image image2Export = layer as Image;
                imageExporter.Export(image2Export, fileStream, optionsBase, Rectangle.Empty);
                fileStream.Flush();
            }
        }
    }

}
