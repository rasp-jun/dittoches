"""Pose-space FK and pole-guided IK without repeated dependency-graph updates.

All vectors are in the armature's canonical X / -Y-forward / Z-up space.
Only the existing pose bones are modified; geometry and weights are untouched.
"""
import math
from mathutils import Matrix,Quaternion,Vector,Euler


class Pose:
    def __init__(self,rig):
        self.rig=rig;self.bones=rig.pose.bones;self.cache={}
        self.relative={b.name:(b.parent.matrix_local.inverted()@b.matrix_local if b.parent else b.matrix_local.copy()) for b in rig.data.bones}

    def reset(self,base=None):
        for bone in self.bones:
            bone.rotation_mode='QUATERNION'
            bone.location=(0,0,0);bone.rotation_quaternion=Quaternion();bone.scale=(1,1,1)
            if base and bone.name in base:
                loc,rotation,scale=base[bone.name]
                bone.location=loc;bone.rotation_quaternion=rotation;bone.scale=scale
        self.cache.clear()

    def capture(self):
        return {b.name:(b.location.copy(),b.rotation_quaternion.copy(),b.scale.copy()) for b in self.bones}

    def basis(self,name):
        bone=self.bones[name]
        return self.world(bone.parent.name)@self.relative[name] if bone.parent else self.relative[name].copy()

    def world(self,name):
        if name not in self.cache:
            b=self.bones[name]
            self.cache[name]=self.basis(name)@Matrix.LocRotScale(b.location,b.rotation_quaternion,b.scale)
        return self.cache[name]

    def point(self,name):return self.world(name).translation.copy()

    def rotate_quat(self,name,delta):
        if name not in self.bones:return
        basis=self.basis(name).to_quaternion()
        bone=self.bones[name]
        bone.rotation_quaternion=(basis.inverted()@delta@basis@bone.rotation_quaternion).normalized()
        self.cache.clear()

    def rotate(self,name,x=0,y=0,z=0):
        if not any((x,y,z)):return
        self.rotate_quat(name,Euler(tuple(math.radians(v) for v in (x,y,z)),'XYZ').to_quaternion())

    def move(self,name,delta):
        if name not in self.bones:return
        self.bones[name].location+=self.basis(name).to_3x3().inverted()@Vector(delta)
        self.cache.clear()

    def scale(self,name,value):
        if name not in self.bones:return
        self.bones[name].scale=value;self.cache.clear()

    def orientation(self,name,rotation):
        self.bones[name].rotation_quaternion=(self.basis(name).to_quaternion().inverted()@rotation).normalized()
        self.cache.clear()

    def aim(self,name,child,position):
        start=self.point(name);old=self.point(child)-start;new=Vector(position)-start
        if old.length>1e-6 and new.length>1e-6:self.rotate_quat(name,old.rotation_difference(new))

    def bend(self,upper,lower,end,fallback=(0,-1,0)):
        a=self.point(upper);b=self.point(lower);d=self.point(end)-a
        if d.length<1e-6:return Vector(fallback)
        d.normalize();pole=b-a;length=pole.length;pole-=d*pole.dot(d)
        return pole.normalized() if pole.length>max(1e-5,length*.02) else Vector(fallback)

    def ik(self,upper,lower,end,target,pole=(0,-1,0),orientation=None):
        if end not in self.bones:return
        a=self.point(upper);b=self.point(lower);c=self.point(end);v=Vector(target)-a
        if v.length<1e-6:return
        l1=(b-a).length;l2=(c-b).length
        distance=max(abs(l1-l2)+.0001,min(v.length,(l1+l2)*.998))
        d=v.normalized();along=(l1*l1-l2*l2+distance*distance)/(2*distance)
        bend=Vector(pole);bend-=d*bend.dot(d)
        if bend.length<1e-5:
            bend=b-a; bend-=d*bend.dot(d)
        if bend.length<1e-5:bend=d.orthogonal()
        knee=a+d*along+bend.normalized()*math.sqrt(max(0,l1*l1-along*along))
        self.aim(upper,lower,knee);self.aim(lower,end,a+d*distance)
        if orientation is not None:self.orientation(end,orientation)
