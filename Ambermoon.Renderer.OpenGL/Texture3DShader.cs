﻿/*
 * Texture3DShader.cs - Shader for textured 3D objects
 *
 * Copyright (C) 2020-2021  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Ambermoon.net.
 *
 * Ambermoon.net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ambermoon.net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Ambermoon.net. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Ambermoon.Renderer
{
    internal class Texture3DShader : ColorShader
    {
        internal static readonly string DefaultTexCoordName = TextureShader.DefaultTexCoordName;
        internal static readonly string DefaultSamplerName = TextureShader.DefaultSamplerName;
        internal static readonly string DefaultAtlasSizeName = TextureShader.DefaultAtlasSizeName;
        internal static readonly string DefaultTexEndCoordName = "texEndCoord";
        internal static readonly string DefaultTexSizeName = "texSize";
        internal static readonly string DefaultPaletteName = TextureShader.DefaultPaletteName;
        internal static readonly string DefaultPaletteIndexName = TextureShader.DefaultPaletteIndexName;
        internal static readonly string DefaultAlphaName = "alpha";
        internal static readonly string DefaultLightName = "light";

        // The palette has a size of 32xNumPalettes pixels.
        // Each row represents one palette of 32 colors.
        // So the palette index determines the pixel row.
        // The column is the palette color index from 0 to 31.
        static string[] Texture3DFragmentShader(State state) => new string[]
        {
            GetFragmentShaderHeader(state),
            $"uniform sampler2D {DefaultSamplerName};",
            $"uniform sampler2D {DefaultPaletteName};",
            $"uniform float {DefaultLightName};",
            $"in vec2 varTexCoord;",
            $"flat in float palIndex;",
            $"flat in vec2 textureEndCoord;",
            $"flat in vec2 textureSize;",
            $"flat in float alphaEnabled;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 realTexCoord = varTexCoord;",
            $"    if (realTexCoord.x >= textureEndCoord.x)",
            $"        realTexCoord.x -= int((textureSize.x + realTexCoord.x - textureEndCoord.x) / textureSize.x) * textureSize.x;",
            $"    if (realTexCoord.y >= textureEndCoord.y)",
            $"        realTexCoord.y -= int((textureSize.y + realTexCoord.y - textureEndCoord.y) / textureSize.y) * textureSize.y;",
            $"    float colorIndex = texture({DefaultSamplerName}, realTexCoord).r * 255.0f;",
            $"    vec4 pixelColor = texture({DefaultPaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {Shader.PaletteCount}));",
            $"    ",
            $"    if (alphaEnabled > 0.5f && (colorIndex < 0.5f || pixelColor.a < 0.5f) || {DefaultLightName} < 0.01f)",
            $"        discard;",
            $"    else",
            $"        {DefaultFragmentOutColorName} = vec4(max(vec3(0), pixelColor.rgb + vec3({DefaultLightName}) - 1), pixelColor.a);",
            $"}}"
        };

        static string[] Texture3DVertexShader(State state) => new string[]
        {
            GetVertexShaderHeader(state),
            $"in vec3 {DefaultPositionName};",
            $"in ivec2 {DefaultTexCoordName};",
            $"in ivec2 {DefaultTexEndCoordName};",
            $"in ivec2 {DefaultTexSizeName};",
            $"in uint {DefaultPaletteIndexName};",
            $"in uint {DefaultAlphaName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"out vec2 varTexCoord;",
            $"flat out float palIndex;",
            $"flat out vec2 textureEndCoord;",
            $"flat out vec2 textureSize;",
            $"flat out float alphaEnabled;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    varTexCoord = atlasFactor * vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    palIndex = float({DefaultPaletteIndexName});",
            $"    textureEndCoord = atlasFactor * vec2({DefaultTexEndCoordName}.x, {DefaultTexEndCoordName}.y);",
            $"    textureSize = atlasFactor * vec2({DefaultTexSizeName}.x, {DefaultTexSizeName}.y);",
            $"    alphaEnabled = {DefaultAlphaName};",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4({DefaultPositionName}, 1.0f);",
            $"}}"
        };

        Texture3DShader(State state)
            : this(state, Texture3DFragmentShader(state), Texture3DVertexShader(state))
        {

        }

        protected Texture3DShader(State state, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(state, fragmentShaderLines, vertexShaderLines)
        {

        }

        public void SetSampler(int textureUnit = 0)
        {
            shaderProgram.SetInput(DefaultSamplerName, textureUnit);
        }

        public void SetPalette(int textureUnit = 1)
        {
            shaderProgram.SetInput(DefaultPaletteName, textureUnit);
        }

        public void SetAtlasSize(uint width, uint height)
        {
            shaderProgram.SetInputVector2(DefaultAtlasSizeName, width, height);
        }

        public void SetLight(float light)
        {
            shaderProgram.SetInput(DefaultLightName, light);
        }

        public new static Texture3DShader Create(State state) => new Texture3DShader(state);
    }
}
