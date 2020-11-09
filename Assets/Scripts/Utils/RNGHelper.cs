using System.Collections.Generic;
using UnityEngine;


public class RNGHelper {

  System.Random random;

  public RNGHelper (int seed) {
    random = new System.Random(seed);
  }

  public float nextDouble () {
    // One way to return a doubble inclusive of 1 https://stackoverflow.com/a/52439575
    const double maxExclusive = 1.0000000004656612875245796924106;
		return (float) (random.NextDouble () * maxExclusive);
  }
}