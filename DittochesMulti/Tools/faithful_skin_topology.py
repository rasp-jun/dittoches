"""Bind detached details as units and diffuse weights over welded surface edges.

The positional weld is used for weight calculation only: UVs, vertex colours,
normals and the original render topology remain untouched.
"""
import numpy as np


def topology(mesh, xyz):
    points, inverse = np.unique(np.round(xyz, 5), axis=0, return_inverse=True)
    edges = np.empty(len(mesh.edges) * 2, dtype=np.int32)
    mesh.edges.foreach_get('vertices', edges)
    edges = np.unique(np.sort(inverse[edges.reshape(-1, 2)], axis=1), axis=0)
    parent = list(range(len(points)))
    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]; a = parent[a]
        return a
    for a, b in edges:
        ra, rb = find(int(a)), find(int(b))
        if ra != rb: parent[rb] = ra
    labels = np.array([find(i) for i in range(len(points))])
    _, labels = np.unique(labels, return_inverse=True)
    return points, inverse, edges, labels


def refine(ident, obj, xyz, weights, names, starts, ends):
    points, inverse, edges, labels = topology(obj.data, xyz)
    counts = np.bincount(inverse)
    surface = np.zeros((len(points), len(names)))
    for k in range(len(names)):
        surface[:, k] = np.bincount(inverse, weights=weights[:, k]) / counts
    rigid = np.zeros(len(points), dtype=bool)
    details = []
    for label in np.unique(labels):
        ids = np.flatnonzero(labels == label); pts = points[ids]
        center = pts.mean(axis=0); span = np.ptp(pts, axis=0)
        # A body/limb component retains its continuous skin. Detached small
        # teeth, nails, feathers, armour plates and eyes move as a single piece.
        fixed = (len(ids) < 2400 and max(span) < .55) or (len(ids) < 200 and max(span) < .65)
        bone = int(np.argmax(surface[ids].sum(axis=0)))
        if ident=='holyangemon' and span[1]>1.3 and span[0]<.15:
            bone=names.index('WingR' if center[0]>0 else 'WingL');fixed=True
        if ident=='holyangemon' and len(ids) in (155,164) and span[2]>.6:
            prefix='FrontUpperWing' if center[2]>1.8 else 'FrontLowerWing'
            bone=names.index(prefix+('R' if center[0]>0 else 'L'));fixed=True
        if ident=='holyangemon' and len(ids)==176 and span[0]>2:
            bone=names.index('Spine');fixed=True
        if ident=='seraphimon' and center[1]>.12 and span[0]>.35:
            bone=names.index('WingR' if center[0]>0 else 'WingL');fixed=True
        if ident == 'agumon' and obj.name.startswith('Object_7'):
            suffix = 'R' if center[0] > 0 else 'L'
            bone = names.index('Jaw' if center[2] > 1.4 else ('Foot' if center[2] < .23 else 'Hand') + suffix)
            fixed = True
        if ident=='greymon' and max(span)<.2 and abs(center[0])>.43 and center[2]>.35:
            bone=names.index('HandR' if center[0]>0 else 'HandL');fixed=True
        if ident=='greymon' and pts[:,2].min()>1.80:
            bone=names.index('Head');fixed=True
        if ident == 'wargreymon' and len(ids) < 3000 and abs(center[0]) > .39:
            suffix = 'R' if center[0] > 0 else 'L'
            candidates = [names.index(n + suffix) for n in ['UpperArm', 'Forearm', 'Hand']]
            bone = min(candidates, key=lambda k: np.linalg.norm(center - (starts[k] + ends[k]) * .5))
            fixed = center[2] > .35
        if fixed:
            surface[ids] = 0; surface[ids, bone] = 1; rigid[ids] = True
        if len(ids) > 10:
            details.append({'vertices': len(ids), 'center': center.tolist(), 'span': span.tolist(), 'rigid': bool(fixed), 'bone': names[bone]})
    # Remove sharp weight boundaries on connected skin. Both copies of a UV
    # seam receive precisely the same resulting weights.
    a, b = edges.T
    degree = np.bincount(np.r_[a, b], minlength=len(points))
    for _ in range(24):
        for k in range(len(names)):
            neighbor = (np.bincount(a, weights=surface[b, k], minlength=len(points)) +
                        np.bincount(b, weights=surface[a, k], minlength=len(points))) / np.maximum(degree, 1)
            surface[~rigid, k] = surface[~rigid, k] * .35 + neighbor[~rigid] * .65
    return surface[inverse], details
