// Blend from the displayed pose, including an interrupted transition. Only the
// destination action runs, so rapidly changing motions cannot accumulate actions.
export class PoseTransition {
  constructor(root) {
    this.nodes = [];
    root.traverse((node) => this.nodes.push(node));
    this.buffer = this.capture();
    this.history = this.capture();
    this.previous = this.capture();
    this.historyDelta = 0;
    this.historyReady = false;
    this.clear();
  }

  capture(buffer) {
    if (buffer) {
      this.nodes.forEach((node, i) => {
        buffer[i].position.copy(node.position); buffer[i].quaternion.copy(node.quaternion);
        buffer[i].scale.copy(node.scale);
        node.morphTargetInfluences?.forEach((v, j) => { buffer[i].morphs[j] = v; });
      });
      return buffer;
    }
    return this.nodes.map((node) => ({
      position: node.position.clone(), quaternion: node.quaternion.clone(),
      scale: node.scale.clone(), morphs: node.morphTargetInfluences?.slice(),
    }));
  }

  restore(pose) {
    if (!pose) return;
    this.nodes.forEach((node, i) => {
      node.position.copy(pose[i].position);
      node.quaternion.copy(pose[i].quaternion);
      node.scale.copy(pose[i].scale);
      pose[i].morphs?.forEach((value, j) => { node.morphTargetInfluences[j] = value; });
    });
  }

  clear() {
    this.restore(this.target);
    this.source = this.target = null;
    this.elapsed = 0;
    this.duration = 0;
    this.velocity = null;
  }

  resetHistory() { this.historyReady = false; this.historyDelta = 0; }

  record(delta) {
    if (delta <= 0) { this.resetHistory(); return; }
    const swap = this.previous; this.previous = this.history; this.history = swap;
    this.capture(this.history);
    this.historyDelta = this.historyReady ? delta : 0;
    this.historyReady = true;
  }

  captureVelocity() {
    if (this.historyDelta <= 0) return null;
    return this.history.map((pose, i) => {
      const previous = this.previous[i];
      const linear = pose.position.clone().sub(previous.position).divideScalar(this.historyDelta);
      // Extrapolate only a short distance to avoid overshoot on large impacts.
      linear.clampLength(0, 1.5);
      const rotation = previous.quaternion.clone().invert().multiply(pose.quaternion);
      if (rotation.w < 0) rotation.set(-rotation.x, -rotation.y, -rotation.z, -rotation.w);
      return { linear, rotation, interval: this.historyDelta };
    });
  }

  begin(source, duration = .22, velocity = null) {
    this.source = source;
    this.target = null;
    this.elapsed = 0;
    this.duration = duration;
    this.velocity = velocity;
    this.apply(0);
  }

  beforeUpdate() {
    // Three caches unchanged animation tracks. Restore the unblended target so
    // a held keyframe never blends back into last frame's blended result.
    this.restore(this.target);
  }

  apply(delta) {
    if (!this.source) return;
    this.elapsed = Math.min(this.duration, this.elapsed + delta);
    if (this.elapsed >= this.duration) { this.source = this.target = null; return; }
    this.target = this.capture(this.buffer);
    const u = this.elapsed / this.duration;
    const weight = u * u * u * (u * (u * 6 - 15) + 10);
    this.nodes.forEach((node, i) => {
      const from = this.source[i], to = this.target[i];
      node.position.lerpVectors(from.position, to.position, weight);
      node.quaternion.slerpQuaternions(from.quaternion, to.quaternion, weight);
      const velocity = this.velocity?.[i];
      if (velocity) {
        // Preserve outgoing velocity at entry, then smoothly hand over to the
        // destination. No per-frame pose/vector/quaternion allocations.
        const travel = this.duration * (u - u*u + u*u*u/3);
        node.position.addScaledVector(velocity.linear, travel * (1-weight));
        const angle = 2*Math.acos(Math.min(1, Math.abs(velocity.rotation.w)));
        const step = Math.min(travel/velocity.interval, angle > 1e-8 ? .3/angle : 0);
        // Quaternion power for a bounded continuation of the previous rotation.
        const half = angle*.5, sine = Math.sin(half);
        if (sine > 1e-8) {
          const gain = Math.sin(half*step)/sine;
          this.turn ??= node.quaternion.clone();
          this.projected ??= node.quaternion.clone();
          this.turn.set(velocity.rotation.x*gain,velocity.rotation.y*gain,velocity.rotation.z*gain,Math.cos(half*step));
          this.projected.copy(from.quaternion).multiply(this.turn);
          node.quaternion.slerpQuaternions(this.projected,to.quaternion,weight);
        }
      }
      node.scale.lerpVectors(from.scale, to.scale, weight);
      from.morphs?.forEach((value, j) => {
        node.morphTargetInfluences[j] = value + (to.morphs[j] - value) * weight;
      });
    });
  }
}
