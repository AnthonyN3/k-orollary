-- main = print (distance (1, 1, 1) (2, 2, 2))

import Data.Char
import Data.Foldable
import Data.List
import Data.List.Split as S
import Data.Maybe
import Data.Sequence
import System.Environment
import System.IO (isEOF)
import Text.Read

main :: IO ()
main = do
  args <- getArgs
  pixels <- getInput
  let k = read (head args)
  let initialCentroids = getInitialCentroids pixels k
  let centroids = kMeans pixels initialCentroids
  output pixels centroids

-- get the initial centroids for the kmeans algorithm
getInitialCentroids :: [(Int, Int, Int)] -> Int -> [(Int, Int, Int)]
getInitialCentroids points k = getInitialCentroidsHelper points k k

getInitialCentroidsHelper :: [(Int, Int, Int)] -> Int -> Int -> [(Int, Int, Int)]
getInitialCentroidsHelper points 0 n = []
getInitialCentroidsHelper points k n = (points !! sampleIndex) : getInitialCentroidsHelper points (k - 1) n
  where
    sampleIndex = floor (fromIntegral (k - 1) / fromIntegral n * fromIntegral (Data.List.length points))

-- read each line of stdin until eof is reached, for each line
-- split the pixel into it's components and add it to a list of pixels
getInput :: IO [(Int, Int, Int)]
getInput = do
  end <- isEOF
  if end
    then return []
    else do
      input <- getLine
      let rgbString = S.splitOn " " input
      let rgbList = map rInt rgbString
      let pixel = tuplify3 rgbList
      next <- getInput
      return (pixel : next)

output :: [(Int, Int, Int)] -> [(Int, Int, Int)] -> IO ()
output [] centroids = do return ()
output (pixel : pixels) centroids = do
  let finalPixel = roundPixel pixel centroids
  putStrLn (stringify finalPixel)
  output pixels centroids

-- read a string into an int
rInt :: String -> Int
rInt = read

-- round pixel to the nearest centroid
roundPixel :: (Int, Int, Int) -> [(Int, Int, Int)] -> (Int, Int, Int)
roundPixel pixel centroids = centroids !! getCluster pixel centroids

-- the kmeans algorithm, takes a list of pixels, as well as initial centroids
-- and determines where the center of each cluster lies
kMeans :: [(Int, Int, Int)] -> [(Int, Int, Int)] -> [(Int, Int, Int)]
kMeans pixels centroids = if centroids == newCentroids then centroids else nextCentroids
  where
    clusters = fromFunction (Data.List.length centroids) (const [])
    newCentroids = calculateCentroids pixels centroids clusters
    nextCentroids = kMeans pixels newCentroids

-- calculate centroids
calculateCentroids :: [(Int, Int, Int)] -> [(Int, Int, Int)] -> Seq [(Int, Int, Int)] -> [(Int, Int, Int)]
calculateCentroids [pixel] centroids clusters = getCentroids updatedClusters
  where
    pCluster = getCluster pixel centroids
    updatedClusters = update pCluster (pixel : index clusters pCluster) clusters
calculateCentroids (pixel : xs) centroids clusters = calculateCentroids xs centroids updatedClusters
  where
    pCluster = getCluster pixel centroids
    updatedClusters = update pCluster (pixel : index clusters pCluster) clusters

-- determine which cluster a pixel belongs to based on a set of centroids
getCluster :: (Int, Int, Int) -> [(Int, Int, Int)] -> Int
getCluster pixel centroids = fromMaybe 0 (elemIndex (minimum distances) distances)
  where
    distances = map (distance pixel) centroids

-- get the centroids from a set of data
getCentroids :: Seq [(Int, Int, Int)] -> [(Int, Int, Int)]
getCentroids sequence = map centre (toList sequence)

-- sum a list of points together
sumPoints :: [(Int, Int, Int)] -> (Int, Int, Int)
sumPoints [] = (0, 0, 0)
sumPoints [x] = x
sumPoints (x : xs) = sum3color x (sumPoints xs)

-- get the centre point of a cluster
centre :: [(Int, Int, Int)] -> (Int, Int, Int)
centre list = div3color (sumPoints list) (Data.List.length list)

-- divide each channel of a color by a given integer
div3color :: (Int, Int, Int) -> Int -> (Int, Int, Int)
div3color (r, g, b) 0 = (r, g, b)
div3color (r, g, b) n = (div r n, div g n, div b n)

-- sum two colors together
sum3color :: (Int, Int, Int) -> (Int, Int, Int) -> (Int, Int, Int)
sum3color (r1, g1, b1) (r2, g2, b2) = (r1 + r2, g1 + g2, b1 + b2)

-- get the distance between two colors
distance :: (Int, Int, Int) -> (Int, Int, Int) -> Int
distance (r1, g1, b1) (r2, g2, b2) = r' * r' + g' * g' + b' * b'
  where
    r' = r1 - r2
    g' = g1 - g2
    b' = b1 - b2

-- transform a list of 3 items into a 3-tuple
tuplify3 :: [Int] -> (Int, Int, Int)
tuplify3 [x, y, z] = (x, y, z)

-- turn a color into a string with each channel separated by spaces
stringify :: (Int, Int, Int) -> String
stringify (r, g, b) = show r ++ " " ++ show g ++ " " ++ show b
