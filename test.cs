using Firebase.Auth;
using System.Threading.Tasks;
public class Test {
    public void M() {
        Task<FirebaseUser> t = null;
        t.ContinueWith(task => { var user = task.Result; });
    }
}
