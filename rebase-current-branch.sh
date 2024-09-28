cb=$(git status|egrep "(On branch|Auf Branch) (.*)$" -o|sed -nr "s/(On branch|Auf Branch) (.*)$/\2/p")
mb=$(git remote show origin | grep 'HEAD' | cut -d':' -f2 | sed -e 's/^ *//g' -e 's/ *$//g')
echo Rebasing branch "'"$cb"'" on "'"$mb"'"
git fetch
git switch $mb
git pull
git switch $cb
git rebase $mb
echo "Press <Enter>" && read -p "$*"
