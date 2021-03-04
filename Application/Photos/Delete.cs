using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Core;
using Application.interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Photos
{
  public class Delete
  {
    public class Command : IRequest<Result<Unit>>
    {
      public string Id { get; set; }
    }

    public class Handler : IRequestHandler<Command, Result<Unit>>
    {
      private readonly DataContext _context;
      private readonly IPhotoAccessor _photoAccessor;
      private readonly IUserAccessor _userAccessor;
      public Handler(DataContext context, IPhotoAccessor photoAccessor, IUserAccessor userAccessor)
      {
        _userAccessor = userAccessor;
        _photoAccessor = photoAccessor;
        _context = context;
      }

      public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
      {
        //ユーザの特定
        //いない場合のエラー処理
        //データベースに渡す前なので非同期にする必要がない
        var user = await _context.Users.Include(p => p.Photos)
            .FirstOrDefaultAsync(x => x.UserName == _userAccessor.GetUsername());
        if (user == null) return null;
        var photo = user.Photos.FirstOrDefault(x => x.Id == request.Id);

        //エラー処理
        if (photo == null) return null;

        if (photo.IsMain) return Result<Unit>.Failure("You cannot delete your main photo");

        var result = await _photoAccessor.DeletePhoto(photo.Id);

        if (result == null) return Result<Unit>.Failure("Problem deleteing photo from Cloudinary");

        //本処理　削除の実行
        user.Photos.Remove(photo);
        var success = await _context.SaveChangesAsync() > 0;
        if (success) return Result<Unit>.Success(Unit.Value);

        //エラー処理
        return Result<Unit>.Failure("Problem deleteing photo from API");
      }
    }
  }
}