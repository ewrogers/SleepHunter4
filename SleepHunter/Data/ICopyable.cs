using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SleepHunter.Data
{
   public interface ICopyable<T>
   {
      void CopyTo(T other);
      void CopyTo(T other, bool copyId);
      void CopyTo(T other, bool copyId, bool copyTimestamp);
   }
}
