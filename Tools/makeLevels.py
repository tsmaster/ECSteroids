import csv

def tryConvertToInt(s):
    s = s.strip()
    if len(s) == 0:
        return 0
    return int(s)

levelDict = {}

with open("levels.csv") as lvl:
    reader = csv.reader(lvl)
    for rownum, row in enumerate(reader):
        if rownum == 0:
            print "header:", row
        else:
            levelNum = int(row[0])
            levelName = row[1]
            numAsteroids = tryConvertToInt(row[2])
            numStatCoins = tryConvertToInt(row[3])
            timeLimitSeconds = tryConvertToInt(row[4])
            numMobileCoins = tryConvertToInt(row[5])
            numSaucersBig = tryConvertToInt(row[6])
            numSaucersSmall = tryConvertToInt(row[7])
            print "read wave desc:", levelNum
            print "wave name: ", levelName
            print "num asteroids: ", numAsteroids

            levelDesc = {'level num' : levelNum,
                         'level name' : levelName,
                         'numAsteroids' : numAsteroids,
                         'numStatCoins' : numStatCoins,
                         'timeLimitSeconds' : timeLimitSeconds,
                         'numMobileCoins' : numMobileCoins,
                         'numSaucersBig' : numSaucersBig,
                         'numSaucersSmall' : numSaucersSmall}

            levelDict[levelNum] = levelDesc
            

    
keys = sorted(levelDict.keys())

for k in keys:
    desc = levelDict[k]
    print '    makeLevelDesc(world, {0}, "{1}", {2}, {3}, {4}, {5}, {6}, {7});'.format(
        desc['level num'], desc['level name'], desc['numAsteroids'], desc['numStatCoins'],
        desc['timeLimitSeconds'], desc['numMobileCoins'], desc['numSaucersBig'], desc['numSaucersSmall'])
