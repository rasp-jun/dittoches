// Local presentation materials: keep source textures, geometry, skin and UVs.
export function finishCharacter(model, renderer, id) {
  const seen = new Set();
  const shell = ['kabuterimon', 'atlur', 'herakle', 'tentomon', 'kuwagamon'].includes(id);
  const soft = ['koromon', 'mochimon', 'tokomon', 'patamon'].includes(id);
  model.traverse(node => {
    if (!node.isMesh) return;
    for (const material of Array.isArray(node.material) ? node.material : [node.material]) {
      if (!material || seen.has(material)) continue;
      seen.add(material);
      for (const value of Object.values(material)) {
        if (value?.isTexture) value.anisotropy = Math.min(8, renderer.capabilities.getMaxAnisotropy());
      }
      if (!material.isMeshStandardMaterial) continue;
      material.envMapIntensity = .55;
      if (id === 'metalgarurumon' || id === 'wargreymon' && /^blinn/.test(material.name)) {
        material.metalness = .62;
        material.roughness = .36;
        material.envMapIntensity = .85;
      } else if (shell) {
        // A glossy chitin surface is dielectric, rather than painted metal.
        material.metalness = .08;
        material.roughness = .43;
      } else {
        material.roughness = Math.min(material.roughness, soft ? .58 : .72);
        if (id === 'garurumon') { material.metalness = 0; material.roughness = .82; }
        if (id === 'rosemon') {
          material.roughness = material.name === 'Skin' ? .62 : material.name === 'Tiferet' ? .32 : .74;
          if (material.name === 'Tiferet') material.metalness = .55;
        }
      }
    }
  });
  return {materials:seen.size, revision:'20261006-surface-finish'};
}
